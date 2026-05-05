using UnityEngine;
using System.Collections.Generic;

/**
 * Test class to demonstrate and verify multiple CSG operations (Subtract and Union).
 */
public class CSGTest : CSGModel
{
    public MeshFilter meshFilterA;
    public List<MeshFilter> extraFilters;

    /** Performs a sequential Subtract operation (A - B1 - B2 - ...) and updates the mesh. */
    public void DoMultipleSubtract()
    {
        List<CSGPolygon> currentPolys = MeshToPolygons(meshFilterA.sharedMesh, meshFilterA.transform);

        foreach(var filterB in extraFilters) 
        {
            if (filterB == null) continue;

            CSGNode nodeA = new CSGNode(currentPolys);
            CSGNode nodeB = new CSGNode(MeshToPolygons(filterB.sharedMesh, filterB.transform));

            nodeA.Invert();
            nodeA.ClipTo(nodeB);
            nodeB.ClipTo(nodeA);
            nodeA.Invert();
            nodeB.Invert();

            currentPolys = nodeA.AllPolygons();
            currentPolys.AddRange(nodeB.AllPolygons());
        }        

        GetComponent<MeshFilter>().mesh = PolygonsToMesh(currentPolys);
    }

    /** Performs a sequential Union operation (A + B1 + B2 + ...) and updates the mesh. */
    public void DoMultipleUnion()
    {
        List<CSGPolygon> currentPolys = MeshToPolygons(meshFilterA.sharedMesh, meshFilterA.transform);

        foreach (var filterB in extraFilters)
        {
            if (filterB == null) continue;

            CSGNode nodeA = new CSGNode(currentPolys);
            CSGNode nodeB = new CSGNode(MeshToPolygons(filterB.sharedMesh, filterB.transform));

            nodeA.ClipTo(nodeB);
            nodeB.ClipTo(nodeA);
            nodeB.Invert();
            nodeB.ClipTo(nodeA);
            nodeB.Invert();

            currentPolys = nodeA.AllPolygons();
            currentPolys.AddRange(nodeB.AllPolygons());
        }

        GetComponent<MeshFilter>().mesh = PolygonsToMesh(currentPolys);
    }
}