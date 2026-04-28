using UnityEngine;
using System.Collections.Generic;

public class CSGTest : CSGModel
{
    public MeshFilter meshFilterA;
    public List<MeshFilter> extraFilters;

[ContextMenu("Subtract Multiple")]
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

    [ContextMenu("Union Multiple")]
    public void DoMultipleUnion()
    {
        List<CSGPolygon> currentPolys = MeshToPolygons(meshFilterA.sharedMesh, meshFilterA.transform);

        foreach (var filterB in extraFilters)
        {
            if (filterB == null) continue;

            CSGNode nodeA = new CSGNode(currentPolys);
            CSGNode nodeB = new CSGNode(MeshToPolygons(filterB.sharedMesh, filterB.transform));

            // Union (A + B)
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