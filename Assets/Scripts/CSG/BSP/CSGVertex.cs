using UnityEngine;
using System.Collections.Generic;

/**
 * Represents a single vertex in a CSG polygon, including position, normal, and UV coordinates.
 */
[System.Serializable]
public struct CSGVertex
{
    public Vector3f position;
    public Vector3f normal;
    public Vector3f uv;

    public CSGVertex(Vector3f pos, Vector3f norm, Vector3f uvCoords)
    {
        position = pos;
        normal = norm;
        uv = uvCoords;
    }    

    /** Linearly interpolates between two vertices for splitting polygons. */
    public static CSGVertex Lerp(CSGVertex a, CSGVertex b, float t)
    {
        return new CSGVertex(
            Vector3f.Lerp(a.position, b.position, t),
            Vector3f.Lerp(a.normal, b.normal, t),
            Vector3f.Lerp(a.uv, b.uv, t)
        );
    }

    /** Helper to create a CSGVertex from Unity's Vector3 types. */
    public static CSGVertex fromVector3(Vector3 pos, Vector3 norm, Vector3 uv)
    {
        return new CSGVertex(
            new Vector3f(pos.x, pos.y, pos.z),
            new Vector3f(norm.x, norm.y, norm.z),
            new Vector3f(uv.x, uv.y, uv.z)
        );
    }    
}
