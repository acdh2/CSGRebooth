using UnityEngine;
using System.Collections.Generic;
using System;

/**
 * Side classification for a vertex relative to a plane.
 */
public enum VertexSide { On, Front, Back }

/**
 * Represents a polygon in CSG operations, consisting of vertices and a supporting plane.
 */
public class CSGPolygon
{
    public List<CSGVertex> vertices;
    public Planef plane;

    // Static buffers for GC efficiency. Safe for single-threaded sequential use only.
    // Not safe for multi-threading or async operations.
    // Static buffers to minimize GC allocations during splitting
    private static readonly List<VertexSide> _sideBuffer = new List<VertexSide>(32);
    private static readonly List<float> _distBuffer = new List<float>(32);
    private static readonly List<CSGVertex> _fVertBuffer = new List<CSGVertex>(32);
    private static readonly List<CSGVertex> _bVertBuffer = new List<CSGVertex>(32);
    private Bounds? _cachedBounds;

    /** Constructor creating a polygon from a list of vertices (winding order determines normal). */
    public CSGPolygon(List<CSGVertex> vels) 
    {
        vertices = vels;
        plane = new Planef(vertices[0].position, vertices[1].position, vertices[2].position);
    }

    /** Constructor creating a polygon from vertices and an explicit normal. */
    public CSGPolygon(List<CSGVertex> vels, Vector3f normal)
    {
        vertices = vels;
        plane = new Planef(normal, vertices[0].position);
    }

    /** Returns the axis-aligned bounding box of this polygon. */
    public Bounds GetBounds()
    {
        if (_cachedBounds.HasValue) return _cachedBounds.Value;

        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        foreach (var v in vertices)
        {
            min.x = Math.Min(min.x, v.position.x);
            min.y = Math.Min(min.y, v.position.y);
            min.z = Math.Min(min.z, v.position.z);
            max.x = Math.Max(max.x, v.position.x);
            max.y = Math.Max(max.y, v.position.y);
            max.z = Math.Max(max.z, v.position.z);
        }
        _cachedBounds = new Bounds();
        _cachedBounds.Value.SetMinMax(min, max);
        return _cachedBounds.Value;
    }    

    /** Reverses the winding order and normal of the polygon. */
    public void Flip()
    {
        vertices.Reverse();
        for (int i = 0; i < vertices.Count; i++)
        {
            var v = vertices[i];
            v.normal = -v.normal;
            vertices[i] = v;
        }
        plane.distance = -plane.distance;
        plane.normal = -plane.normal;
    }

    /** Splits this polygon by a plane and sorts the resulting fragments into front, back, or coplanar lists. */
    public void Split(Planef splitPlane, List<CSGPolygon> fList, List<CSGPolygon> bList, List<CSGPolygon> fCoplanar, List<CSGPolygon> bCoplanar)
    {
        _sideBuffer.Clear();
        _distBuffer.Clear();

        bool hasFront = false;
        bool hasBack = false;

        // 1. Pre-calculate distances and classify vertices
        for (int i = 0; i < vertices.Count; i++)
        {
            float dist = splitPlane.GetDistanceToPoint(vertices[i].position);
            _distBuffer.Add(dist);

            if (dist > CSGConfig.Epsilon)
            {
                _sideBuffer.Add(VertexSide.Front);
                hasFront = true;
            }
            else if (dist < -CSGConfig.Epsilon)
            {
                _sideBuffer.Add(VertexSide.Back);
                hasBack = true;
            }
            else
            {
                _sideBuffer.Add(VertexSide.On);
            }
        }

        // Case A: Polygon lies entirely on the plane (Coplanar)
        if (!hasFront && !hasBack)
        {
            float dot = Vector3f.Dot(this.plane.normal, splitPlane.normal);
            if (dot > 0) fCoplanar.Add(this);
            else bCoplanar.Add(this);
            return;
        }

        // Case B: Polygon lies entirely in front of the plane
        if (!hasBack)
        {
            fList.Add(this);
            return;
        }

        // Case C: Polygon lies entirely behind the plane
        if (!hasFront)
        {
            bList.Add(this);
            return;
        }

        // Case D: Polygon is spanning the plane, split it into two new polygons
        _fVertBuffer.Clear();
        _bVertBuffer.Clear();

        for (int i = 0; i < vertices.Count; i++)
        {
            int j = i + 1;
            if (j == vertices.Count) j = 0;

            CSGVertex vi = vertices[i];
            CSGVertex vj = vertices[j];
            VertexSide si = _sideBuffer[i];
            VertexSide sj = _sideBuffer[j];
            float di = _distBuffer[i];
            float dj = _distBuffer[j];

            if (si != VertexSide.Back) _fVertBuffer.Add(vi);
            if (si != VertexSide.Front) _bVertBuffer.Add(vi);

            if ((si == VertexSide.Front && sj == VertexSide.Back) || (si == VertexSide.Back && sj == VertexSide.Front))
            {
                // Use pre-calculated distances for the interpolation factor t
                float t = Mathf.Abs(di) / (Mathf.Abs(di) + Mathf.Abs(dj));
                CSGVertex intersect = CSGVertex.Lerp(vi, vj, t);

                _fVertBuffer.Add(intersect);
                _bVertBuffer.Add(intersect);
            }
        }

        if (_fVertBuffer.Count >= 3) fList.Add(new CSGPolygon(new List<CSGVertex>(_fVertBuffer), plane.normal));
        if (_bVertBuffer.Count >= 3) bList.Add(new CSGPolygon(new List<CSGVertex>(_bVertBuffer), plane.normal));
    }
}
