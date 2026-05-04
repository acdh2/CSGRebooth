using System.Collections.Generic;
using UnityEngine;
using System;

public class CSGNode
{
    public List<CSGPolygon> polygons = new List<CSGPolygon>();
    public Planed partition;
    public CSGNode front;
    public CSGNode back;
    public Bounds nodeBounds; 

    private static readonly List<CSGPolygon> _fCopBuffer = new List<CSGPolygon>(64);
    private static readonly List<CSGPolygon> _bCopBuffer = new List<CSGPolygon>(64);

    public CSGNode(List<CSGPolygon> list = null)
    {
        if (list != null && list.Count > 0) Build(list);
    }

    public void Build(List<CSGPolygon> list)
    {
        if (list == null || list.Count == 0) return;

        // Bepaal splitter
        CSGPolygon splitter = (list.Count > 20) 
            ? list[UnityEngine.Random.Range(0, list.Count)] 
            : FindBestSplitter(list);

        this.partition = splitter.plane;
        
        List<CSGPolygon> fList = new List<CSGPolygon>();
        List<CSGPolygon> bList = new List<CSGPolygon>();

        for (int i = 0; i < list.Count; i++)
        {
            list[i].Split(this.partition, fList, bList, this.polygons, this.polygons);
        }

        if (fList.Count > 0)
        {
            if (this.front == null) this.front = new CSGNode();
            this.front.Build(fList);
        }

        if (bList.Count > 0)
        {
            if (this.back == null) this.back = new CSGNode();
            this.back.Build(bList);
        }
        
        UpdateBounds();
    }

    // De snelle manier om nieuwe polygonen in de bestaande boom te weven
    public void InjectPolygons(List<CSGPolygon> newPolys)
    {
        if (newPolys == null || newPolys.Count == 0) return;

        List<CSGPolygon> f = new List<CSGPolygon>();
        List<CSGPolygon> b = new List<CSGPolygon>();

        foreach (var poly in newPolys)
        {
            poly.Split(this.partition, f, b, this.polygons, this.polygons);
        }

        if (f.Count > 0)
        {
            if (front != null) front.InjectPolygons(f);
            else front = new CSGNode(f);
        }
        if (b.Count > 0)
        {
            if (back != null) back.InjectPolygons(b);
            else back = new CSGNode(b);
        }
        UpdateBounds();
    }

    public void ClipPolygons(List<CSGPolygon> input, List<CSGPolygon> output)
    {
        if (input.Count == 0) return;

        // Early exit: als de input de bounds van deze node totaal niet raakt
        // hoeven we niet te splitten, alles is dan 'buiten' (front) t.o.v. deze subtree.
        // Dit bespaart duizenden berekeningen bij de ster.
        Bounds inputBounds = CalculateListBounds(input);
        if (!this.nodeBounds.Intersects(inputBounds))
        {
            output.AddRange(input);
            return;
        }

        List<CSGPolygon> f = new List<CSGPolygon>();
        List<CSGPolygon> b = new List<CSGPolygon>();

        foreach (var poly in input)
        {
            _fCopBuffer.Clear();
            _bCopBuffer.Clear();
            poly.Split(this.partition, f, b, _fCopBuffer, _bCopBuffer);
            f.AddRange(_fCopBuffer);
            b.AddRange(_bCopBuffer);
        }

        if (this.front != null) this.front.ClipPolygons(f, output);
        else output.AddRange(f);

        if (this.back != null) this.back.ClipPolygons(b, output);
    }

    public void ClipTo(CSGNode other)
    {
        // Gebruik bounds om hele takken over te slaan
        if (!this.nodeBounds.Intersects(other.nodeBounds)) return;

        List<CSGPolygon> clipped = new List<CSGPolygon>();
        other.ClipPolygons(this.polygons, clipped);
        this.polygons = clipped;

        front?.ClipTo(other);
        back?.ClipTo(other);
        UpdateBounds();
    }

    public void Subtract(CSGNode other)
    {
        this.Invert();
        this.ClipTo(other);
        other.ClipTo(this);
        other.Invert();
        other.ClipTo(this);
        other.Invert();
        
        // Gebruik Inject ipv Build voor snelheid
        this.InjectPolygons(other.AllPolygons());
        this.Invert();
        UpdateBounds();
    }

    public void Union(CSGNode other)
    {
        this.ClipTo(other);
        other.ClipTo(this);
        other.Invert();
        other.ClipTo(this);
        other.Invert();
        this.InjectPolygons(other.AllPolygons());
        UpdateBounds();
    }

    public void Invert()
    {
        foreach (var poly in polygons) poly.Flip();
        partition.normal = -partition.normal;
        partition.distance = -partition.distance;
        front?.Invert();
        back?.Invert();
        var temp = front; front = back; back = temp;
    }

    public void UpdateBounds()
    {
        this.nodeBounds = CalculateListBounds(this.polygons);
        if (front != null) this.nodeBounds.Encapsulate(front.nodeBounds);
        if (back != null) this.nodeBounds.Encapsulate(back.nodeBounds);
    }

    private Bounds CalculateListBounds(List<CSGPolygon> list)
    {
        if (list.Count == 0) return new Bounds();
        Bounds b = list[0].GetBounds();
        for (int i = 1; i < list.Count; i++) b.Encapsulate(list[i].GetBounds());
        return b;
    }

    private CSGPolygon FindBestSplitter(List<CSGPolygon> list)
    {
        CSGPolygon best = list[0];
        long bestScore = long.MaxValue;
        int sampleCount = Math.Min(list.Count, 30);
        int step = Math.Max(1, list.Count / sampleCount);

        for (int i = 0; i < list.Count; i += step)
        {
            CSGPolygon candidate = list[i];
            int splits = 0, front = 0, back = 0;
            foreach (var p in list)
            {
                var side = candidate.plane.Compare(p);
                if (side == CSGSide.Spanning) splits++;
                else if (side == CSGSide.Front) front++;
                else if (side == CSGSide.Back) back++;
            }
            long score = (splits * 15) + Math.Abs(front - back);
            if (score < bestScore) { bestScore = score; best = candidate; if (splits == 0) break; }
        }
        return best;
    }

    public List<CSGPolygon> AllPolygons()
    {
        List<CSGPolygon> list = new List<CSGPolygon>();
        FillPolygonList(list);
        return list;
    }

    private void FillPolygonList(List<CSGPolygon> list)
    {
        list.AddRange(polygons);
        front?.FillPolygonList(list);
        back?.FillPolygonList(list);
    }
}