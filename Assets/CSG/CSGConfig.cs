using UnityEngine;
using System.Collections.Generic;

public static class CSGConfig
{
    public const float Epsilon = 0.001f; // Voor vlak-controles
    public const float SnapGrid = 0.0001f;  // Voor vertex-snapping

    public static Vector3 Snap(Vector3 pos)
    {
        return new Vector3(
            Mathf.Round(pos.x / SnapGrid) * SnapGrid,
            Mathf.Round(pos.y / SnapGrid) * SnapGrid,
            Mathf.Round(pos.z / SnapGrid) * SnapGrid
        );
    }
}