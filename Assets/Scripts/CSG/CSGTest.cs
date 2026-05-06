using UnityEngine;
using System.Collections.Generic;

/**
 * Test class to demonstrate and verify multiple CSG operations (Subtract and Union).
 */
public class CSGTest : MonoBehaviour
{
    /** The base mesh filter used as the starting geometry. */
    public MeshFilter meshFilterA;
    /** A list of additional mesh filters to be used as brushes in operations. */
    public List<MeshFilter> extraFilters;

    /**
     * Executes a series of subtraction operations where all extra filters are carved out of the base mesh.
     */
    public void DoMultipleSubtract()
    {
        // 1. Initialize the stack
        CSGStack stack = new CSGStack();

        // 2. Add the base mesh (the first operation is always the base, so we use Union)
        stack.AddOperation(CSGType.Union, meshFilterA.sharedMesh, meshFilterA.transform.localToWorldMatrix);

        // 3. Add all extra filters as Subtract operations
        foreach(var filterB in extraFilters) 
        {
            if (filterB == null) continue;
            stack.AddOperation(CSGType.Subtract, filterB.sharedMesh, filterB.transform.localToWorldMatrix);
        }        

        // 4. Retrieve the result. The stack handles caching and recalculation.
        // Pass this object's transform to ensure correct local mesh data.
        GetComponent<MeshFilter>().mesh = stack.GetMesh(this.transform, true);
    }    

    /**
     * Executes a series of union operations where all meshes are merged into a single volume.
     */
    public void DoMultipleUnion()
    {
        // 1. Initialize the stack
        CSGStack stack = new CSGStack();

        // 2. Add the base mesh
        stack.AddOperation(CSGType.Union, meshFilterA.sharedMesh, meshFilterA.transform.localToWorldMatrix);

        // 3. Add all extra filters as Union operations
        foreach(var filterB in extraFilters) 
        {
            if (filterB == null) continue;
            stack.AddOperation(CSGType.Union, filterB.sharedMesh, filterB.transform.localToWorldMatrix);
        }        

        // 4. Generate the combined mesh
        GetComponent<MeshFilter>().mesh = stack.GetMesh(this.transform, true);
    }    
}