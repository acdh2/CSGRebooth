using UnityEngine;
using System.Collections.Generic;

/**
 * Base class for CSG models, providing utilities to convert between Unity Meshes and CSG Polygons.
 */
public abstract class CSGModel : MonoBehaviour
{
    /** Generates simple planar UV mapping (XZ projection) for vertices that lack UVs. */
    protected Vector2[] GeneratePlanarUVs(Vector3[] vertices)
    {
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
        }
        return uvs;
    }

    /** Converts a Unity Mesh into a list of CSG polygons, applying the provided transformation. */
    protected List<CSGPolygon> MeshToPolygons(Mesh mesh, Transform tx)
    {
        Vector3[] v = mesh.vertices;
        int[] t = mesh.triangles;
        Vector3[] n = mesh.normals;
        Vector2[] u = mesh.uv;

        if (n == null || n.Length == 0)
        {
            mesh.RecalculateNormals();
            n = mesh.normals;
        }

        if (u == null || u.Length == 0)
        {
            u = GeneratePlanarUVs(v);
        }

        List<CSGPolygon> polygons = new List<CSGPolygon>();
        for (int i = 0; i < t.Length; i += 3)
        {
            List<CSGVertex> verts = new List<CSGVertex>
            {
                CSGVertex.fromVector3(tx.TransformPoint(v[t[i]]), tx.TransformDirection(n[t[i]]), u[t[i]]),
                CSGVertex.fromVector3(tx.TransformPoint(v[t[i+1]]), tx.TransformDirection(n[t[i+1]]), u[t[i+1]]),
                CSGVertex.fromVector3(tx.TransformPoint(v[t[i+2]]), tx.TransformDirection(n[t[i+2]]), u[t[i+2]])
            };
            polygons.Add(new CSGPolygon(verts));
        }

        return polygons;
    }    

    /** Converts a list of CSG polygons back into a Unity Mesh. */
    protected Mesh PolygonsToMesh(List<CSGPolygon> polygons)
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        List<Vector3> outVerts = new List<Vector3>();
        List<Vector3> outNorms = new List<Vector3>();
        List<Vector2> outUvs = new List<Vector2>();
        List<int> outTris = new List<int>();

        foreach (var poly in polygons)
        {
            int baseIndex = outVerts.Count;

            for (int i = 0; i < poly.vertices.Count; i++)
            {
                CSGVertex v = poly.vertices[i];
                outVerts.Add(transform.InverseTransformPoint(v.position.toVector3()));
                outNorms.Add(transform.InverseTransformDirection(v.normal.toVector3()));
                outUvs.Add(v.uv.toVector3());
            }

            // Triangle fan triangulation
            for (int i = 2; i < poly.vertices.Count; i++)
            {
                outTris.Add(baseIndex);
                outTris.Add(baseIndex + i - 1);
                outTris.Add(baseIndex + i);
            }
        }

        mesh.vertices = outVerts.ToArray();
        mesh.normals = outNorms.ToArray();
        mesh.uv = outUvs.ToArray();
        mesh.triangles = outTris.ToArray();
        
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }    
}