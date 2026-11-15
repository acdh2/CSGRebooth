using System.Collections.Generic;
using UnityEngine;
using System;

/**
 * Represents a node in the Binary Space Partitioning (BSP) tree used for CSG operations.
 */
public class CSGNode
{
    /** Polygons stored within this specific node. */
    public List<CSGPolygon> polygons = new List<CSGPolygon>();
    /** The plane used to partition space at this node. */
    public Planef partition;
    /** The child node representing the space in front of the partition plane. */
    public CSGNode front;
    /** The child node representing the space behind the partition plane. */
    public CSGNode back;

    /**
     * Bounding box of the closed solid this tree was built from (root nodes only).
     * It is computed once in the constructor and NOT updated by ClipTo: the solid a BSP tree
     * represents is defined by its planes, which clipping does not change, so the box of the
     * polygons that remain after clipping would be too small.
     */
    private Bounds solidBounds;
    private bool hasSolidBounds;
    /** True while the tree represents the complement of the solid (after an odd number of Invert calls). */
    private bool inverted;

    // Static buffers for GC efficiency. Safe for single-threaded sequential use only.
    private static readonly List<CSGPolygon> _fCopBuffer = new List<CSGPolygon>(64);
    private static readonly List<CSGPolygon> _bCopBuffer = new List<CSGPolygon>(64);

    /** Constructor for root nodes. The polygons must form a closed solid. */
    public CSGNode(List<CSGPolygon> list = null)
    {
        if (list != null && list.Count > 0)
        {
            solidBounds = CalculateListBounds(list);
            // A margin of 4 x Epsilon on every side: the BSP treats anything within Epsilon of a plane as "on" it, so a face
            // touching the solid up to rounding noise must go through the tree, not be kept by this quick test (it left holes).
            solidBounds.Expand(8f * CSGConfig.Epsilon);
            hasSolidBounds = true;
            Build(list);
        }
    }

    /** Internal constructor for child nodes. */
    private CSGNode(List<CSGPolygon> list, bool isChild)
    {
        if (list != null && list.Count > 0) Build(list);
    }

    /** Builds the BSP tree from a list of polygons. */
    public void Build(List<CSGPolygon> list)
    {
        if (list == null || list.Count == 0) return;

        // Choose a splitter only for a fresh node; a node that already has a plane keeps it,
        // otherwise polygons stored here earlier would no longer lie on this node's plane.
        // FindBestSplitter is deterministic, so every client builds the same tree.
        if (this.polygons.Count == 0 && this.front == null && this.back == null)
        {
            this.partition = FindBestSplitter(list).plane;
        }

        List<CSGPolygon> fList = new List<CSGPolygon>();
        List<CSGPolygon> bList = new List<CSGPolygon>();

        for (int i = 0; i < list.Count; i++)
        {
            list[i].Split(this.partition, fList, bList, this.polygons, this.polygons);
        }

        if (fList.Count > 0)
        {
            if (this.front == null) this.front = new CSGNode(null, true);
            this.front.Build(fList);
        }

        if (bList.Count > 0)
        {
            if (this.back == null) this.back = new CSGNode(null, true);
            this.back.Build(bList);
        }
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
            else front = new CSGNode(f, true);
        }
        if (b.Count > 0)
        {
            if (back != null) back.InjectPolygons(b);
            else back = new CSGNode(b, true);
        }
    }

    /**
     * Clips a list of polygons against the solid this tree represents: keeps the parts outside it.
     * Call on a root node. A list entirely outside the solid's bounding box is decided at once:
     * outside a normal solid means keep everything, outside an inverted solid (its complement)
     * means everything lies inside it, so nothing is kept.
     */
    public void ClipPolygons(List<CSGPolygon> input, List<CSGPolygon> output)
    {
        if (input.Count == 0) return;

        if (hasSolidBounds && !solidBounds.Intersects(CalculateListBounds(input)))
        {
            if (!inverted) output.AddRange(input);
            return;
        }

        ClipPolygonsRecursive(input, output);
    }

    private void ClipPolygonsRecursive(List<CSGPolygon> input, List<CSGPolygon> output)
    {
        if (input.Count == 0) return;

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

        if (this.front != null) this.front.ClipPolygonsRecursive(f, output);
        else output.AddRange(f);

        if (this.back != null) this.back.ClipPolygonsRecursive(b, output);
    }

    /** Clips this node's polygons (and its children's) against another tree. Pass a root node. */
    public void ClipTo(CSGNode other)
    {
        if (this.polygons.Count > 0)
        {
            List<CSGPolygon> clipped = new List<CSGPolygon>();
            other.ClipPolygons(this.polygons, clipped);
            this.polygons = clipped;
        }

        front?.ClipTo(other);
        back?.ClipTo(other);
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
        inverted = !inverted;
    }

    /** Calculates the bounding box for a given list of polygons. */
    private static Bounds CalculateListBounds(List<CSGPolygon> list)
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