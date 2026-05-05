using UnityEngine;
using System.Collections.Generic;

/**
 * Base class for CSG models, providing utilities to convert between Unity Meshes and CSG Polygons.
 */
public abstract class CSGModel : MonoBehaviour
{
    /**
     * Generates simple planar UV mapping (XZ projection) for vertices.
     * @param vertices Array of vertex positions in local space.
     * @return Array of UV coordinates projected from X and Z components.
     */
    protected static Vector2[] GeneratePlanarUVs(Vector3[] vertices)
    {
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
        }
        return uvs;
    }

    /**
     * Converts a Unity Mesh into world-space CSG polygons.
     * @param mesh Source Unity Mesh.
     * @param transform Matrix for local-to-world conversion.
     * @return List of CSG polygons.
     */
    public static List<CSGPolygon> MeshToPolygons(Mesh mesh, Matrix4x4 transform)
    {
        List<CSGPolygon> polygons = new List<CSGPolygon>();
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector2[] uvs = mesh.uv;
        int[] triangles = mesh.triangles;

        bool hasNormals = normals != null && normals.Length > 0;
        bool hasUvs = uvs != null && uvs.Length > 0;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            List<CSGVertex> polyVerts = new List<CSGVertex>();
            for (int j = 0; j < 3; j++)
            {
                int index = triangles[i + j];
                Vector3 pos = transform.MultiplyPoint3x4(vertices[index]);
                
                Vector3f normalF = hasNormals 
                    ? Vector3f.fromVector3(transform.MultiplyVector(normals[index]).normalized) 
                    : Vector3f.zero;

                Vector3f uvF = hasUvs 
                    ? new Vector3f(uvs[index].x, uvs[index].y, 0) 
                    : Vector3f.zero;

                polyVerts.Add(new CSGVertex(Vector3f.fromVector3(pos), normalF, uvF));
            }
            polygons.Add(new CSGPolygon(polyVerts));
        }
        return polygons;
    }

    /**
     * Converts CSG polygons back into a Unity Mesh.
     * @param polygons List of polygons in world space.
     * @param worldToLocal Matrix for world-to-local conversion.
     * @param weldVertices If true, merges identical vertices.
     * @return Resulting Unity Mesh.
     */
    public static Mesh PolygonsToMesh(List<CSGPolygon> polygons, Matrix4x4 worldToLocal, bool weldVertices = false)
    {
        Mesh mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        
        var data = new MeshData();
        var cache = new Dictionary<(Vector3, Vector3, Vector2), int>();

        foreach (var poly in polygons)
        {
            int[] indices = ProcessPolygonVertices(poly, worldToLocal, weldVertices, data, cache);
            TriangulateFan(indices, data.triangles);
        }

        data.ApplyTo(mesh);
        return mesh;
    }

    private static int[] ProcessPolygonVertices(CSGPolygon poly, Matrix4x4 worldToLocal, bool weld, MeshData data, Dictionary<(Vector3, Vector3, Vector2), int> cache)
    {
        int[] indices = new int[poly.vertices.Count];
        for (int i = 0; i < poly.vertices.Count; i++)
        {
            CSGVertex v = poly.vertices[i];
            Vector3 pos = worldToLocal.MultiplyPoint3x4(v.position.toVector3());
            Vector3 norm = worldToLocal.MultiplyVector(v.normal.toVector3()).normalized;
            Vector2 uv = new Vector2(v.uv.x, v.uv.y);

            if (weld)
            {
                var key = (pos, norm, uv);
                if (!cache.TryGetValue(key, out int idx))
                {
                    idx = data.AddVertex(pos, norm, uv);
                    cache.Add(key, idx);
                }
                indices[i] = idx;
            }
            else
            {
                indices[i] = data.AddVertex(pos, norm, uv);
            }
        }
        return indices;
    }

    private static void TriangulateFan(int[] indices, List<int> triangles)
    {
        for (int i = 2; i < indices.Length; i++)
        {
            triangles.Add(indices[0]);
            triangles.Add(indices[i - 1]);
            triangles.Add(indices[i]);
        }
    }

    private class MeshData
    {
        public List<Vector3> vertices = new List<Vector3>();
        public List<Vector3> normals = new List<Vector3>();
        public List<Vector2> uvs = new List<Vector2>();
        public List<int> triangles = new List<int>();

        public int AddVertex(Vector3 p, Vector3 n, Vector2 u)
        {
            int index = vertices.Count;
            vertices.Add(p);
            normals.Add(n);
            uvs.Add(u);
            return index;
        }

        public void ApplyTo(Mesh mesh)
        {
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
        }
    }
}