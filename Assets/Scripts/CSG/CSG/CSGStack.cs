using System.Collections.Generic;
using UnityEngine;
using System;

/**
 * Manages a sequence of CSG operations and computes the resulting geometry.
 * Handles the layering of multiple boolean operations to produce a final mesh.
 */
public class CSGStack
{
    private List<CSGOperation> operations = new List<CSGOperation>();
    private bool isDirty = true;
    private List<CSGPolygon> resultPolygons = new List<CSGPolygon>();

    /**
     * Adds a new boolean operation to the stack.
     * @param type The type of CSG operation (Union, Subtract, Intersect).
     * @param mesh The source mesh for the operation.
     * @param transform Transformation matrix for the mesh in world space.
     */
    public void AddOperation(CSGType type, Mesh mesh, Matrix4x4 transform)
    {
        operations.Add(new CSGOperation(type, mesh, transform));
        isDirty = true;
    }

    /**
     * Clears all operations from the stack.
     */
    public void Clear()
    {
        operations.Clear();
        isDirty = true;
    }

    /**
     * Generates a Unity Mesh from the resulting CSG polygons.
     * @param targetTransform The transform used to convert world space polygons back to local space.
     * @param weldVertices If true, merges identical vertices to create a smooth mesh.
     * @return A new Unity Mesh representing the result of all operations.
     */
    public Mesh GetMesh(Transform targetTransform, bool weldVertices)
    {
        if (isDirty) Recalculate();
        
        // Pass the world-to-local matrix to ensure the mesh is centered relative to the target's origin
        return CSGUtils.PolygonsToMesh(resultPolygons, targetTransform.worldToLocalMatrix, weldVertices);
    }

    /**
     * Executes the CSG operations in the stack to update the resulting polygons.
     */
    private void Recalculate()
    {
        if (operations.Count == 0) return;

        // Step 1: Initialize the base (The first operation always serves as the base geometry)
        resultPolygons = PrepareBrush(operations[0]);
        
        // Step 2: Perform sequential operations
        for (int i = 1; i < operations.Count; i++)
        {
            var op = operations[i];
            CSGNode resultNode = new CSGNode(resultPolygons);
            
            // Retrieve the brush node from cache or build it if necessary
            if (op.brushNode == null)
            {
                op.brushNode = new CSGNode(PrepareBrush(op));
            }

            // Execute the specific CSG action
            switch (op.type)
            {
                case CSGType.Subtract:
                    resultNode.Subtract(op.brushNode);
                    break;
                case CSGType.Union:
                    resultNode.Union(op.brushNode);
                    break;
                case CSGType.Intersect:
                    resultNode.Intersect(op.brushNode);
                    break;
            }

            resultPolygons = resultNode.AllPolygons();
        }

        isDirty = false;
    }

    /**
     * Converts a CSG operation's mesh data into a list of polygons.
     * @param op The operation containing the mesh and transformation data.
     * @return A list of polygons in world space.
     */
    private List<CSGPolygon> PrepareBrush(CSGOperation op)
    {
        return CSGUtils.MeshToPolygons(op.mesh, op.localToWorld);
    }
}