using UnityEngine;
using System.Collections.Generic;

public abstract class CSGModel : MonoBehaviour
{
    // Maakt CSGPolygonen van een Unity Mesh
    protected List<CSGPolygon> MeshToPolygons(Mesh mesh, Transform tx)
    {
        List<CSGPolygon> polygons = new List<CSGPolygon>();
        Vector3[] v = mesh.vertices;
        Vector3[] n = mesh.normals;
        Vector2[] u = mesh.uv;
        int[] t = mesh.triangles;

        for (int i = 0; i < t.Length; i += 3)
        {
            List<CSGVertex> verts = new List<CSGVertex>
            {
                new CSGVertex(tx.TransformPoint(v[t[i]]), tx.TransformDirection(n[t[i]]), u[t[i]]),
                new CSGVertex(tx.TransformPoint(v[t[i+1]]), tx.TransformDirection(n[t[i+1]]), u[t[i+1]]),
                new CSGVertex(tx.TransformPoint(v[t[i+2]]), tx.TransformDirection(n[t[i+2]]), u[t[i+2]])
            };
            polygons.Add(new CSGPolygon(verts));
        }
        return polygons;
    }

    // Zet CSGPolygonen terug naar een Unity Mesh
    protected Mesh PolygonsToMesh2(List<CSGPolygon> polygons)
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<Vector3> norms = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        foreach (var poly in polygons)
        {
            // Eenvoudige fan-triangulatie voor de polygonen
            int startIdx = verts.Count;
            for (int i = 0; i < poly.vertices.Count; i++)
            {
                verts.Add(transform.InverseTransformPoint(poly.vertices[i].position));
                norms.Add(transform.InverseTransformDirection(poly.vertices[i].normal));
                uvs.Add(poly.vertices[i].uv);
            }

            for (int i = 2; i < poly.vertices.Count; i++)
            {
                tris.Add(startIdx + 0);
                tris.Add(startIdx + i - 1);
                tris.Add(startIdx + i);
            }
        }

        mesh.vertices = verts.ToArray();
        mesh.normals = norms.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        return mesh;
    }
    protected Mesh PolygonsToMesh(List<CSGPolygon> polygons, bool weld = true)
    {
        Mesh mesh = new Mesh();
        List<Vector3> outVerts = new List<Vector3>();
        List<Vector3> outNorms = new List<Vector3>();
        List<Vector2> outUvs = new List<Vector2>();
        List<int> outTris = new List<int>();

        // Dictionary voor welding (alleen gebruikt als weld == true)
        Dictionary<string, int> vertexCache = new Dictionary<string, int>();

        foreach (var poly in polygons)
        {
            List<int> polyIndices = new List<int>();

            for (int i = 0; i < poly.vertices.Count; i++)
            {
                CSGVertex v = poly.vertices[i];
                Vector3 localPos = transform.InverseTransformPoint(v.position);
                Vector3 localNorm = transform.InverseTransformDirection(v.normal);

                int index = 0;
                bool found = false;

                if (weld)
                {
                    // De sleutel bepaalt wat we 'hetzelfde' vinden. 
                    // Positie, Normaal en UV moeten matchen voor een naadloze weld.
                    string key = string.Format("{0:F4}_{1:F4}_{2:F4}_{3:F4}_{4:F4}_{5:F4}_{6:F4}_{7:F4}", 
                        localPos.x, localPos.y, localPos.z, 
                        localNorm.x, localNorm.y, localNorm.z, 
                        v.uv.x, v.uv.y);

                    if (vertexCache.TryGetValue(key, out index))
                    {
                        found = true;
                    }
                }

                if (!found)
                {
                    index = outVerts.Count;
                    outVerts.Add(localPos);
                    outNorms.Add(localNorm);
                    outUvs.Add(v.uv);
                    if (weld) vertexCache.Add(string.Format("{0:F4}_{1:F4}_{2:F4}_{3:F4}_{4:F4}_{5:F4}_{6:F4}_{7:F4}", 
                        localPos.x, localPos.y, localPos.z, 
                        localNorm.x, localNorm.y, localNorm.z, 
                        v.uv.x, v.uv.y), index);
                }
                
                polyIndices.Add(index);
            }

            // Triangulatie
            for (int i = 2; i < polyIndices.Count; i++)
            {
                outTris.Add(polyIndices[0]);
                outTris.Add(polyIndices[i - 1]);
                outTris.Add(polyIndices[i]);
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