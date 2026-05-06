using UnityEngine;
using System;

/**
* A 3D vector using floats, used for CSG calculations.
*/
[System.Serializable]
public struct Vector3f
{
    public float x;
    public float y;
    public float z;

    /** Constructor using x, y, and z coordinates. */
    public Vector3f(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    /** Returns the length of the vector. */
    public float Magnitude => Mathf.Sqrt(x * x + y * y + z * z);

    /** Returns a vector with the same direction but a magnitude of 1. */
    public Vector3f normalized
    {
        get
        {
            float mag = Magnitude;
            return mag > float.Epsilon ? new Vector3f(x / mag, y / mag, z / mag) : new Vector3f(0, 0, 0);
        }
    }

    /** Linearly interpolates between two vectors. */
    public static Vector3f Lerp(Vector3f a, Vector3f b, float t)
    {
        t = Mathf.Max(0, Mathf.Min(1, t));
        
        return new Vector3f(
            a.x + (b.x - a.x) * t,
            a.y + (b.y - a.y) * t,
            a.z + (b.z - a.z) * t
        );
    }

    /** Negates the vector. */
    public static Vector3f operator -(Vector3f v)
    {
        return new Vector3f(-v.x, -v.y, -v.z);
    }    

    public static readonly Vector3f zero = new Vector3f(0, 0, 0);

    public static bool operator ==(Vector3f a, Vector3f b)
    {
        return a.x == b.x && a.y == b.y && a.z == b.z;
    }

    public static bool operator !=(Vector3f a, Vector3f b)
    {
        return !(a == b);
    }

    public override bool Equals(object obj) => obj is Vector3f other && this == other;
    public override int GetHashCode() => (x, y, z).GetHashCode();

    /** Calculates the dot product of two vectors. */
    public static float Dot(Vector3f lhs, Vector3f rhs)
    {
        return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
    }

    /** Calculates the cross product of two vectors. */
    public static Vector3f Cross(Vector3f lhs, Vector3f rhs)
    {
        return new Vector3f(
            lhs.y * rhs.z - lhs.z * rhs.y,
            lhs.z * rhs.x - lhs.x * rhs.z,
            lhs.x * rhs.y - lhs.y * rhs.x
        );
    }

    /** Returns a vector containing the minimum components of two vectors. */
    public static Vector3f Min(Vector3f a, Vector3f b)
    {
        return new Vector3f(Math.Min(a.x, b.x), Math.Min(a.y, b.y), Math.Min(a.z, b.z));
    }

    /** Returns a vector containing the maximum components of two vectors. */
    public static Vector3f Max(Vector3f a, Vector3f b)
    {
        return new Vector3f(Math.Max(a.x, b.x), Math.Max(a.y, b.y), Math.Max(a.z, b.z));
    }    

    /** Converts this Vector3f to a Unity Vector3. */
    public Vector3 toVector3()
    {
        return new Vector3((float)x, (float)y, (float)z);
    }

    /** Creates a Vector3f from a Unity Vector3. */
    public static Vector3f fromVector3(Vector3 source)
    {
        return new Vector3f(source.x, source.y, source.z);
    }
}
