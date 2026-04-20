using UnityEngine;
using System.Collections.Generic;

public struct CSGPlane
{
    public Vector3 normal;
    public double distance;

    public CSGPlane(Vector3 normal, Vector3 point)
    {
        this.normal = normal.normalized;
        this.distance = Vector3.Dot(this.normal, point);
    }

    // Bepaalt aan welke kant van de plane een punt ligt
    public double DistanceTo(Vector3 point)
    {
        return Vector3.Dot(normal, point) - distance;
    }
}