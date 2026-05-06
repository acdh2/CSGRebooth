using System.Collections.Generic;
using UnityEngine;
using System;

/**
 * Defines the type of Constructive Solid Geometry (CSG) boolean operation to perform.
 */
public enum CSGType 
{ 
    /** Combines two meshes into one. */
    Union, 
    /** Subtracts the brush volume from the base mesh. */
    Subtract, 
    /** Keeps only the volume where both meshes overlap. */
    Intersect 
}

/**
 * Represents a single CSG operation containing the operation type, source mesh, 
 * and transformation data.
 */
[System.Serializable]
public class CSGOperation
{
    /** The type of boolean operation to be executed. */
    public CSGType type;
    /** The source Unity Mesh used as the brush. */
    public Mesh mesh;
    /** Transformation matrix converting the mesh from local space to world space. */
    public Matrix4x4 localToWorld;
    
    /** Cached BSP tree representing the geometry of this specific brush. */
    public CSGNode brushNode;

    /**
     * Constructor for a CSG operation.
     * 
     * @param type The boolean operation type.
     * @param mesh The source mesh for the operation.
     * @param transform The transformation matrix for the mesh.
     */
    public CSGOperation(CSGType type, Mesh mesh, Matrix4x4 transform)
    {
        this.type = type;
        this.mesh = mesh;
        this.localToWorld = transform;
    }
}