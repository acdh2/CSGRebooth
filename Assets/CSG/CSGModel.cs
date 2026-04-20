using UnityEngine;
using System.Collections.Generic;

public abstract class CSGModel : MonoBehaviour
{
    // Maakt CSGPolygonen van een Unity Mesh
    protected Vector2[] GeneratePlanarUVs(Vector3[] vertices)
    {
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            // Simpele XY-projectie (Planar Mapping)
            uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
        }
        return uvs;
    }

    protected Mesh WeldMesh(Mesh mesh)
    {
        Mesh weldedMesh = Instantiate(mesh);
        Vector3[] vertices = weldedMesh.vertices;
        int[] triangles = weldedMesh.triangles;

        Dictionary<Vector3, int> vertexCache = new Dictionary<Vector3, int>();
        List<int> newTriangles = new List<int>();
        List<Vector3> newVertices = new List<Vector3>();
        
        // We gebruiken hier alleen positie om te welden. 
        // Als je ook op UV wilt welden, moet de key complexer.
        for (int i = 0; i < triangles.Length; i++)
        {
            Vector3 v = vertices[triangles[i]];
            if (!vertexCache.TryGetValue(v, out int index))
            {
                index = newVertices.Count;
                newVertices.Add(v);
                vertexCache.Add(v, index);
            }
            newTriangles.Add(index);
        }

        weldedMesh.Clear();
        weldedMesh.vertices = newVertices.ToArray();
        weldedMesh.triangles = newTriangles.ToArray();
        weldedMesh.RecalculateNormals(); // Essentieel na welding
        weldedMesh.RecalculateBounds();
        
        return weldedMesh;
    }

    protected List<CSGPolygon> MeshToPolygons(Mesh mesh, Transform tx, bool weld = false)
    {
        // Indien weld gewenst is, maken we eerst een tijdelijke welded versie
        Mesh processingMesh = weld ? WeldMesh(mesh) : mesh;

        Vector3[] v = processingMesh.vertices;
        int[] t = processingMesh.triangles;
        Vector3[] n = processingMesh.normals;
        Vector2[] u = processingMesh.uv;

        // Safety checks op de (eventueel nieuwe) mesh data
        if (n == null || n.Length == 0)
        {
            processingMesh.RecalculateNormals();
            n = processingMesh.normals;
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
            polygons.Add(new CSGPolygon(verts));//, -Vector3d.fromVector3(n[t[i]])));
        }

        // Ruim de tijdelijke mesh op als we die hebben aangemaakt
        if (weld) Destroy(processingMesh);

        return polygons;
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
                Vector3 localPos = transform.InverseTransformPoint(v.position.toVector3());
                Vector3 localNorm = transform.InverseTransformDirection(v.normal.toVector3());

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
                    outUvs.Add(v.uv.toVector3());
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