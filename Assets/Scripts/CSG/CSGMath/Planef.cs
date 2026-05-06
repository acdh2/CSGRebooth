using UnityEngine;
using System;

/**
* Represents a plane in 3D space using float precision.
*/
[System.Serializable]
public struct Planef
{
    public Vector3f normal;
    public float distance;

    /** Constructor using a normal vector and distance from origin. */
    public Planef(Vector3f normal, float distance)
    {
        this.normal = normal.normalized;
        this.distance = distance;
    }

    /** Constructor using a normal and a point on the plane. */
    public Planef(Vector3f inNormal, Vector3f inPoint)
    {
        this.normal = inNormal.normalized;
        this.distance = -Vector3f.Dot(this.normal, inPoint);
    }

    /** Constructor that calculates the plane from three points (winding order determines normal). */
    public Planef(Vector3f a, Vector3f b, Vector3f c)
    {
        Vector3f side1 = new Vector3f(b.x - a.x, b.y - a.y, b.z - a.z);
        Vector3f side2 = new Vector3f(c.x - a.x, c.y - a.y, c.z - a.z);

        float nx = side1.y * side2.z - side1.z * side2.y;
        float ny = side1.z * side2.x - side1.x * side2.z;
        float nz = side1.x * side2.y - side1.y * side2.x;

        this.normal = new Vector3f(nx, ny, nz).normalized;
        this.distance = -Vector3f.Dot(this.normal, a);
    }  

    /** Returns the signed distance from the plane to a point. */
    public float GetDistanceToPoint(Vector3f point)
    {
        return Vector3f.Dot(normal, point) + distance;
    }  

    /** Determines which side of the plane a bounding box is on. */
    public CSGSide Compare(Bounds bounds)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        float radius = Math.Abs(normal.x * extents.x) + 
                        Math.Abs(normal.y * extents.y) + 
                        Math.Abs(normal.z * extents.z);

        float distance = GetDistanceToPoint(Vector3f.fromVector3(center));

        if (distance > radius + CSGConfig.Epsilon) return CSGSide.Front;
        if (distance < -radius - CSGConfig.Epsilon) return CSGSide.Back;

        return CSGSide.Spanning;
    }    

    /** Determines which side of the plane a polygon is on. */
    public CSGSide Compare(CSGPolygon poly)
    {
        int front = 0;
        int back = 0;

        foreach (var v in poly.vertices)
        {
            float d = GetDistanceToPoint(v.position);
            
            if (d > CSGConfig.Epsilon) front++;
            else if (d < -CSGConfig.Epsilon) back++;
        }

        if (front > 0 && back > 0) return CSGSide.Spanning;
        if (front > 0) return CSGSide.Front;
        if (back > 0) return CSGSide.Back;
        return CSGSide.On;
    }        
}    
