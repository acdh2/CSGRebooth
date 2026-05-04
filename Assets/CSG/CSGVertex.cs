using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct CSGVertex
{
    public Vector3d position;
    public Vector3d normal;
    public Vector3d uv;

    public CSGVertex(Vector3d pos, Vector3d norm, Vector3d uvCoords)
    {
        position = pos;
        normal = norm;
        uv = uvCoords;
    }    

    // Lineaire interpolatie voor splitsingen
    public static CSGVertex Lerp(CSGVertex a, CSGVertex b, float t)
    {
        return new CSGVertex(
            Vector3d.Lerp(a.position, b.position, t),
            Vector3d.Lerp(a.normal, b.normal, t).normalized,
            Vector3d.Lerp(a.uv, b.uv, t)
        );
    }

    public static CSGVertex fromVector3(Vector3 pos, Vector3 norm, Vector3 uv)
    {
        return new CSGVertex(
            new Vector3d(pos.x, pos.y, pos.z),
            new Vector3d(norm.x, norm.y, norm.z),
            new Vector3d(uv.x, uv.y, uv.z)
        );
    }    
}
