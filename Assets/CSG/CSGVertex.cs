using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct CSGVertex
{
    public Vector3 position;
    public Vector3 normal;
    public Vector2 uv;

    public CSGVertex(Vector3 pos, Vector3 norm, Vector2 uvCoords)
    {
        position = CSGConfig.Snap(pos);
        normal = norm;
        uv = uvCoords;
    }    

    // Lineaire interpolatie voor splitsingen
    public static CSGVertex Lerp(CSGVertex a, CSGVertex b, float t)
    {
        return new CSGVertex(
            Vector3.Lerp(a.position, b.position, t),
            Vector3.Lerp(a.normal, b.normal, t).normalized,
            Vector2.Lerp(a.uv, b.uv, t)
        );
    }
}
