using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/**
 * Self-tests for the CSG library. Run them with the menu CSG > Run self-tests.
 * Each test builds boxes from polygons, performs an operation and checks the
 * volume and surface area of the result against the exact answer.
 */
public static class CSGSelfTest
{
    [MenuItem("CSG/Run self-tests")]
    public static void RunAll()
    {
        int failed = 0;
        failed += Check("Subtract, partly overlapping", Op(Box(0, 0, 0, 2, 2, 2), Box(1, 1, 1, 3, 3, 3), CSGType.Subtract), 7f, 24f);
        failed += Check("Subtract, brush fully inside", Op(Box(0, 0, 0, 3, 3, 3), Box(1, 1, 1, 2, 2, 2), CSGType.Subtract), 26f, 60f);
        failed += Check("Union, faces exactly touching", Op(Box(0, 0, 0, 1, 1, 1), Box(1, 0, 0, 2, 1, 1), CSGType.Union), 2f, 10f);
        failed += Check("Union, far apart", Op(Box(0, 0, 0, 1, 1, 1), Box(5, 0, 0, 6, 1, 1), CSGType.Union), 2f, 12f);
        failed += Check("Intersect, partly overlapping", Op(Box(0, 0, 0, 2, 2, 2), Box(1, 1, 1, 3, 3, 3), CSGType.Intersect), 1f, 6f);
        failed += Check("Window in wall, exactly as thick", Op(Box(0, 0, 0, 4, 0.2f, 3), Box(1.5f, 0, 1, 2.5f, 0.2f, 2), CSGType.Subtract), 2.2f, 25.6f);
        failed += Check("Window in wall, brush sticks out", Op(Box(0, 0, 0, 4, 0.2f, 3), Box(1.5f, -0.1f, 1, 2.5f, 0.3f, 2), CSGType.Subtract), 2.2f, 25.6f);
        failed += CheckDeterminism();
        failed += CheckStackRecalculate();

        if (failed == 0) Debug.Log("CSG self-tests: all passed");
        else Debug.LogError($"CSG self-tests: {failed} failed");
    }

    // ---------------------------------------------------------------- tests

    private static int Check(string name, List<CSGPolygon> result, float expectVolume, float expectArea)
    {
        float v = Volume(result), a = Area(result);
        bool ok = Mathf.Abs(v - expectVolume) < 1e-3f && Mathf.Abs(a - expectArea) < 1e-3f;
        string msg = $"{(ok ? "PASS" : "FAIL")}  {name}: volume {v:F4} (expected {expectVolume}), area {a:F4} (expected {expectArea}), {result.Count} polygons";
        if (ok) Debug.Log(msg); else Debug.LogError(msg);
        return ok ? 0 : 1;
    }

    /** The same input must give exactly the same polygons, so every client builds the same mesh. */
    private static int CheckDeterminism()
    {
        string first = Fingerprint(BuildPlan());
        string second = Fingerprint(BuildPlan());
        bool ok = first == second;
        string msg = $"{(ok ? "PASS" : "FAIL")}  Determinism: two runs of 30 unions give {(ok ? "identical" : "different")} results";
        if (ok) Debug.Log(msg); else Debug.LogError(msg);
        return ok ? 0 : 1;
    }

    /** Adding an operation and recalculating must give the same result as a fresh stack. */
    private static int CheckStackRecalculate()
    {
        Mesh a = ToMesh(Box(0, 0, 0, 2, 2, 2));
        Mesh b = ToMesh(Box(1, 1, 1, 3, 3, 3));
        Mesh c = ToMesh(Box(-1, 0.5f, 0.5f, 0.5f, 1.5f, 1.5f));
        var go = new GameObject("CSGSelfTestTarget") { hideFlags = HideFlags.HideAndDontSave };

        var incremental = new CSGStack();
        incremental.AddOperation(CSGType.Union, a, Matrix4x4.identity);
        incremental.AddOperation(CSGType.Subtract, b, Matrix4x4.identity);
        incremental.GetMesh(go.transform, false);                  // first recalculation
        incremental.AddOperation(CSGType.Subtract, c, Matrix4x4.identity);
        Mesh m1 = incremental.GetMesh(go.transform, false);        // second recalculation reuses operations

        var fresh = new CSGStack();
        fresh.AddOperation(CSGType.Union, a, Matrix4x4.identity);
        fresh.AddOperation(CSGType.Subtract, b, Matrix4x4.identity);
        fresh.AddOperation(CSGType.Subtract, c, Matrix4x4.identity);
        Mesh m2 = fresh.GetMesh(go.transform, false);

        Object.DestroyImmediate(go);
        float v1 = MeshVolume(m1), v2 = MeshVolume(m2);
        bool ok = Mathf.Abs(v1 - v2) < 1e-3f && Mathf.Abs(v1 - (8f - 1f - 0.5f)) < 1e-3f;
        string msg = $"{(ok ? "PASS" : "FAIL")}  Stack recalculation: volume {v1:F4} after adding an operation, {v2:F4} fresh (expected 6.5)";
        if (ok) Debug.Log(msg); else Debug.LogError(msg);
        return ok ? 0 : 1;
    }

    // ---------------------------------------------------------------- helpers

    private static List<CSGPolygon> Op(List<CSGPolygon> a, List<CSGPolygon> b, CSGType type)
    {
        var na = new CSGNode(a);
        var nb = new CSGNode(b);
        if (type == CSGType.Union) na.Union(nb);
        else if (type == CSGType.Subtract) na.Subtract(nb);
        else na.Intersect(nb);
        return na.AllPolygons();
    }

    private static List<CSGPolygon> BuildPlan()
    {
        var rnd = new System.Random(42);
        List<CSGPolygon> result = Box(0, 0, 0, 0.2f, 0.2f, 0.2f);
        for (int i = 0; i < 30; i++)
        {
            float x = rnd.Next(0, 30) / 10f, z = rnd.Next(0, 30) / 10f;
            float w = rnd.Next(1, 5) / 10f, d = rnd.Next(1, 5) / 10f, h = rnd.Next(1, 4) / 10f;
            result = Op(result, Box(x, 0, z, x + w, h, z + d), CSGType.Union);
        }
        return result;
    }

    private static string Fingerprint(List<CSGPolygon> polys)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var p in polys)
            foreach (var v in p.vertices)
                sb.Append(v.position.x.ToString("R")).Append(',').Append(v.position.y.ToString("R")).Append(',').Append(v.position.z.ToString("R")).Append(';');
        return polys.Count + ":" + sb.ToString().GetHashCode();
    }

    /** Axis-aligned box from (x0,y0,z0) to (x1,y1,z1) as six outward-facing quads. */
    public static List<CSGPolygon> Box(float x0, float y0, float z0, float x1, float y1, float z1)
    {
        var list = new List<CSGPolygon>();
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var n = Vector3.Cross(b - a, c - a).normalized;
            var verts = new List<CSGVertex>();
            foreach (var p in new[] { a, b, c, d }) verts.Add(CSGVertex.fromVector3(p, n, Vector3.zero));
            list.Add(new CSGPolygon(verts));
        }
        Quad(new Vector3(x0, y0, z0), new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x0, y1, z0));
        Quad(new Vector3(x1, y0, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), new Vector3(x1, y0, z1));
        Quad(new Vector3(x0, y0, z0), new Vector3(x1, y0, z0), new Vector3(x1, y0, z1), new Vector3(x0, y0, z1));
        Quad(new Vector3(x0, y1, z0), new Vector3(x0, y1, z1), new Vector3(x1, y1, z1), new Vector3(x1, y1, z0));
        Quad(new Vector3(x0, y0, z0), new Vector3(x0, y1, z0), new Vector3(x1, y1, z0), new Vector3(x1, y0, z0));
        Quad(new Vector3(x0, y0, z1), new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1));
        return list;
    }

    private static Mesh ToMesh(List<CSGPolygon> polys) => CSGUtils.PolygonsToMesh(polys, Matrix4x4.identity);

    /** Volume via the divergence theorem; only meaningful for a closed, outward-facing surface. */
    private static float Volume(List<CSGPolygon> polys)
    {
        double v = 0;
        foreach (var p in polys)
            for (int i = 1; i < p.vertices.Count - 1; i++)
                v += Triple(p.vertices[0].position.toVector3(), p.vertices[i].position.toVector3(), p.vertices[i + 1].position.toVector3());
        return (float)(v / 6.0);
    }

    private static float MeshVolume(Mesh m)
    {
        var vs = m.vertices; var t = m.triangles; double v = 0;
        for (int i = 0; i < t.Length; i += 3) v += Triple(vs[t[i]], vs[t[i + 1]], vs[t[i + 2]]);
        return (float)(v / 6.0);
    }

    private static double Triple(Vector3 a, Vector3 b, Vector3 c) => Vector3.Dot(a, Vector3.Cross(b, c));

    private static float Area(List<CSGPolygon> polys)
    {
        float a = 0;
        foreach (var p in polys)
        {
            Vector3 sum = Vector3.zero;
            for (int i = 1; i < p.vertices.Count - 1; i++)
                sum += Vector3.Cross(p.vertices[i].position.toVector3() - p.vertices[0].position.toVector3(),
                                     p.vertices[i + 1].position.toVector3() - p.vertices[0].position.toVector3());
            a += sum.magnitude * 0.5f;
        }
        return a;
    }
}
