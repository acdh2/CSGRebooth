using System.Collections.Generic;
using UnityEngine;
using System;


public class CSGNode
{
    public List<CSGPolygon> polygons = new List<CSGPolygon>();
    public Planed partition;
    public CSGNode front;
    public CSGNode back;

    public CSGNode(List<CSGPolygon> list = null)
    {
        if (list != null && list.Count > 0)
        {
            Build(list);
        }
    }

    public void Build(List<CSGPolygon> list)
{
    if (list == null || list.Count == 0) return;

    // 1. Zoek de beste splitter in plaats van altijd list[0]
    CSGPolygon splitter = FindBestSplitter(list);
    this.partition = splitter.plane;
    
    List<CSGPolygon> fList = new List<CSGPolygon>();
    List<CSGPolygon> bList = new List<CSGPolygon>();

    // 2. Verdeel de polygonen
    foreach (var poly in list)
    {
        // De splitter zelf en andere coplanar polygonen gaan naar this.polygons
        poly.Split(this.partition, fList, bList, this.polygons, this.polygons);
    }

    // 3. Bouw recursief verder
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
}

    private CSGPolygon FindBestSplitter(List<CSGPolygon> list)
    {
        CSGPolygon best = list[0];
        long bestScore = long.MaxValue;

        // We testen een steekproef om de snelheid erin te houden
        int sampleStep = Math.Max(1, list.Count / 15); 

        for (int i = 0; i < list.Count; i += sampleStep)
        {
            CSGPolygon candidate = list[i];
            int splits = 0;
            int front = 0;
            int back = 0;

            foreach (var p in list)
            {
                var side = candidate.plane.Compare(p); // Gebruik je bestaande Side-check
                if (side == CSGSide.Spanning) splits++;
                else if (side == CSGSide.Front) front++;
                else if (side == CSGSide.Back) back++;
            }

            // Score: We haten splits (wegens precisiefouten) en willen balans
            long score = (splits * 10) + Math.Abs(front - back);

            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }
        return best;
    }

    // Draait de hele boom om (nodig voor Subtract)
    public void Invert()
    {
        foreach (var poly in polygons) poly.Flip();
        
        partition.normal = -partition.normal;
        partition.distance = -partition.distance;

        front?.Invert();
        back?.Invert();

        // Swap de kinderen
        CSGNode temp = front;
        front = back;
        back = temp;
    }

    public void ClipTo(CSGNode other)
    {
        this.polygons = other.ClipPolygons(this.polygons);

        if (this.front != null) this.front.ClipTo(other);
        if (this.back != null) this.back.ClipTo(other);
    }    

    public List<CSGPolygon> ClipPolygons(List<CSGPolygon> list)
    {
        List<CSGPolygon> f = new List<CSGPolygon>();
        List<CSGPolygon> b = new List<CSGPolygon>();

        foreach (var poly in list)
        {
            List<CSGPolygon> fCop = new List<CSGPolygon>();
            List<CSGPolygon> bCop = new List<CSGPolygon>();
            poly.Split(this.partition, f, b, fCop, bCop);

            f.AddRange(fCop);
            b.AddRange(bCop); 
        }

        if (this.front != null) f = this.front.ClipPolygons(f);
        if (this.back != null) b = this.back.ClipPolygons(b);
        else b.Clear();

        f.AddRange(b);
        return f;
    }    

    public List<CSGPolygon> AllPolygons()
    {
        List<CSGPolygon> list = new List<CSGPolygon>(polygons);
        if (front != null) list.AddRange(front.AllPolygons());
        if (back != null) list.AddRange(back.AllPolygons());
        return list;
    }
}