using System.Collections.Generic;
using UnityEngine;
using System;

/**
 * Represents a node in the Binary Space Partitioning (BSP) tree used for CSG operations.
 */
public class CSGNode
{
    private CSGNode parent;

    /** Polygons stored within this specific node. */
    public List<CSGPolygon> polygons = new List<CSGPolygon>();
    /** The plane used to partition space at this node. */
    public Planef partition;
    /** The child node representing the space in front of the partition plane. */
    public CSGNode front;
    /** The child node representing the space behind the partition plane. */
    public CSGNode back;
    /** Axis-aligned bounding box encompassing this node and all its descendants. */
    public Bounds nodeBounds; 

    // Static buffers for GC efficiency. Safe for single-threaded sequential use only.
    private static readonly List<CSGPolygon> _fCopBuffer = new List<CSGPolygon>(64);
    private static readonly List<CSGPolygon> _bCopBuffer = new List<CSGPolygon>(64);

    /** Constructor for root nodes (without a parent). */
    public CSGNode(List<CSGPolygon> list = null) : this(null, list) { }

    /** Internal constructor to create child nodes with a reference to their parent. */
    private CSGNode(CSGNode parent, List<CSGPolygon> list = null)
    {
        this.parent = parent;
        if (list != null && list.Count > 0) Build(list);
    }

    /** Builds the BSP tree from a list of polygons. */
    public void Build(List<CSGPolygon> list)
    {
        if (list == null || list.Count == 0) return;

        // Choose a splitter to maintain a balanced tree
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
            if (this.front == null) this.front = new CSGNode(this);
            this.front.Build(fList);
        }

        if (bList.Count > 0)
        {
            if (this.back == null) this.back = new CSGNode(this);
            this.back.Build(bList);
        }
        
        UpdateBounds();
    }

    /** Performs a subtraction operation (A - B). */
    public void Subtract(CSGNode other)
    {
        // A - B = A cut by B, and B cut by A (inverted)
        this.Invert();
        this.ClipTo(other);
        other.ClipTo(this);
        other.Invert();
        other.ClipTo(this);
        other.Invert();
        
        // Add remaining brush polygons to the base
        this.Build(other.AllPolygons());
        this.Invert();
    }

    /** Performs an intersection operation (A ∩ B). */
    public void Intersect(CSGNode other)
    {
        this.Invert();
        other.ClipTo(this);
        other.Invert();
        this.ClipTo(other);
        other.ClipTo(this);
        
        this.Build(other.AllPolygons());
        this.Invert();
    }    

    /** Performs a union operation (A + B). */
    public void Union(CSGNode other)
    {
        // A + B = A clipped by B, and B clipped by A (inverted)
        this.ClipTo(other);
        other.ClipTo(this);
        
        other.Invert();
        other.ClipTo(this);
        other.Invert();
        
        // Add remaining brush polygons to the base
        this.Build(other.AllPolygons());
    }

    /** Injects new polygons into the existing BSP tree structure. */
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
            else front = new CSGNode(this, f);
        }
        if (b.Count > 0)
        {
            if (back != null) back.InjectPolygons(b);
            else back = new CSGNode(this, b);
        }
        UpdateBounds();
    }

    /** Recursively clips a list of polygons against this BSP tree. */
    public void ClipPolygons(List<CSGPolygon> input, List<CSGPolygon> output)
    {
        if (input.Count == 0) return;

        // Early exit: if input does not intersect this node's bounds, it's all outside.
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

    /** Clips this node's polygons against another BSP tree. */
    public void ClipTo(CSGNode other)
    {
        if (!this.nodeBounds.Intersects(other.nodeBounds)) return;

        List<CSGPolygon> clipped = new List<CSGPolygon>();
        other.ClipPolygons(this.polygons, clipped);
        this.polygons = clipped;

        front?.ClipTo(other);
        back?.ClipTo(other);
        UpdateBounds();
    }

    /** Inverts the BSP tree by flipping all polygons and swapping front/back children. */
    public void Invert()
    {
        foreach (var poly in polygons) poly.Flip();
        partition.normal = -partition.normal;
        partition.distance = -partition.distance;
        front?.Invert();
        back?.Invert();
        var temp = front; front = back; back = temp;
    }

    /** Updates the axis-aligned bounding box of this node and its children. */
    public void UpdateBounds()
    {
        this.nodeBounds = CalculateListBounds(this.polygons);
        if (front != null) this.nodeBounds.Encapsulate(front.nodeBounds);
        if (back != null) this.nodeBounds.Encapsulate(back.nodeBounds);

        parent?.UpdateBounds();
    }        

    /** Calculates the bounding box for a given list of polygons. */
    private Bounds CalculateListBounds(List<CSGPolygon> list)
    {
        if (list.Count == 0) return new Bounds();
        Bounds b = list[0].GetBounds();
        for (int i = 1; i < list.Count; i++) b.Encapsulate(list[i].GetBounds());
        return b;
    }

    /** Heuristic to find a polygon that minimizes splits and balances the tree. */
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

    /** Returns all polygons contained within this BSP tree. */
    public List<CSGPolygon> AllPolygons()
    {
        List<CSGPolygon> list = new List<CSGPolygon>();
        FillPolygonList(list);
        return list;
    }

    /** Recursively collects all polygons from this node and its children. */
    private void FillPolygonList(List<CSGPolygon> list)
    {
        list.AddRange(polygons);
        front?.FillPolygonList(list);
        back?.FillPolygonList(list);
    }
}