using UnityEngine;
using System;
using System.Collections.Generic;

public static class CSGConfig
{
    public const float Epsilon = 0.00001f; // Voor vlak-controles
    //public const float SnapGrid = 0.001f;  // Voor vertex-snapping

    // public static Vector3d SnapZ(Vector3d pos)
    // {
    //     return new Vector3d(
    //         Mathf.Round(pos.x / SnapGrid) * SnapGrid,
    //         Mathf.Round(pos.y / SnapGrid) * SnapGrid,
    //         Mathf.Round(pos.z / SnapGrid) * SnapGrid
    //     );
    // }
}

[System.Serializable]
public struct Vector3d
{
    public float x;
    public float y;
    public float z;

    // Constructor
    public Vector3d(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }


    public float Magnitude => Mathf.Sqrt(x * x + y * y + z * z);

    public Vector3d normalized
    {
        get
        {
            float mag = Magnitude;
            return mag > 0 ? new Vector3d(x / mag, y / mag, z / mag) : new Vector3d(0, 0, 0);
        }
    }

    public static Vector3d Lerp(Vector3d a, Vector3d b, float t)
    {
        // t tussen 0 en 1 houden
        t = Mathf.Max(0, Mathf.Min(1, t));
        
        return new Vector3d(
            a.x + (b.x - a.x) * t,
            a.y + (b.y - a.y) * t,
            a.z + (b.z - a.z) * t
        );
    }

    public static Vector3d operator -(Vector3d v)
    {
        return new Vector3d(-v.x, -v.y, -v.z);
    }    

    public static readonly Vector3d zero = new Vector3d(0, 0, 0);

    public static bool operator ==(Vector3d a, Vector3d b)
    {
        return a.x == b.x && a.y == b.y && a.z == b.z;
    }

    public static bool operator !=(Vector3d a, Vector3d b)
    {
        return !(a == b);
    }

    // Vergeet niet Equals en GetHashCode voor de volledigheid
    public override bool Equals(object obj) => obj is Vector3d other && this == other;
    public override int GetHashCode() => (x, y, z).GetHashCode();

    public static float Dot(Vector3d lhs, Vector3d rhs)
    {
        return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
    }

    public static Vector3d Cross(Vector3d lhs, Vector3d rhs)
    {
        return new Vector3d(
            lhs.y * rhs.z - lhs.z * rhs.y,
            lhs.z * rhs.x - lhs.x * rhs.z,
            lhs.x * rhs.y - lhs.y * rhs.x
        );
    }

    public Vector3 toVector3()
    {
        return new Vector3((float)x, (float)y, (float)z);
    }

    public static Vector3d fromVector3(Vector3 source)
    {
        return new Vector3d(source.x, source.y, source.z);
    }

}

public enum CSGSide 
{ 
    Front, 
    Back, 
    On, 
    Spanning 
}

[System.Serializable]
public struct Planed
{
    public Vector3d normal;
    public float distance;

    // Constructor op basis van normaal en afstand
    public Planed(Vector3d normal, float distance)
    {
        this.normal = normal.normalized;
        this.distance = distance;
    }

    // Constructor op basis van een punt en een normaal
    public Planed(Vector3d inNormal, Vector3d inPoint)
    {
        this.normal = inNormal.normalized;
        // De afstand d = dot(normal, point)
        this.distance = -(normal.x * inPoint.x + normal.y * inPoint.y + normal.z * inPoint.z);
    }

    public Planed(Vector3d a, Vector3d b, Vector3d c)
    {
        // Bereken twee vectoren die op het vlak liggen
        Vector3d side1 = new Vector3d(b.x - a.x, b.y - a.y, b.z - a.z);
        Vector3d side2 = new Vector3d(c.x - a.x, c.y - a.y, c.z - a.z);

        // Kruisproduct voor de normaal: (side1.y * side2.z - side1.z * side2.y, ...)
        float nx = side1.y * side2.z - side1.z * side2.y;
        float ny = side1.z * side2.x - side1.x * side2.z;
        float nz = side1.x * side2.y - side1.y * side2.x;

        this.normal = new Vector3d(nx, ny, nz).normalized;
        
        // De afstand d = -(normal . a)
        this.distance = -(this.normal.x * a.x + this.normal.y * a.y + this.normal.z * a.z);
    }  

    public float GetDistanceToPoint(Vector3d point)
    {
        // De afstand is (Normal · Point) + Distance
        return (normal.x * point.x + normal.y * point.y + normal.z * point.z) + distance;
    }  

    public CSGSide Compare(CSGPolygon poly)
    {
        int front = 0;
        int back = 0;

        foreach (var v in poly.vertices)
        {
            float d = Vector3d.Dot(this.normal, v.position) - this.distance;
            
            if (d < -CSGConfig.Epsilon) front++;
            else if (d > -CSGConfig.Epsilon) back++;
        }

        if (front > 0 && back > 0) return CSGSide.Spanning;
        if (front > 0) return CSGSide.Front;
        if (back > 0) return CSGSide.Back;
        return CSGSide.On;
    }
}    
