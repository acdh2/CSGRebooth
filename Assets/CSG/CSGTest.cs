using UnityEngine;
using System.Collections.Generic;

public class CSGTest : CSGModel
{
    public MeshFilter meshFilterA;
    public List<MeshFilter> extraFilters;

    private float currentTime;
    private void StartDeltaTime()
    {
        currentTime = Time.realtimeSinceStartup;
    }

    private void WriteDeltaTime(string msg)
    {
        float now = Time.realtimeSinceStartup;
        float deltaTime = now - currentTime;
        currentTime = now;
        Debug.Log(deltaTime + " :: " + msg);
    }

    [ContextMenu("Subtract Multiple")]
    public void DoMultipleSubtract()
    {
        StartDeltaTime();
        List<CSGPolygon> currentPolys = MeshToPolygons(meshFilterA.sharedMesh, meshFilterA.transform);

        foreach(var filterB in extraFilters) 
        {
            if (filterB == null) continue;

            CSGNode nodeA = new CSGNode(currentPolys);
            WriteDeltaTime("creating nodeA"); 
            CSGNode nodeB = new CSGNode(MeshToPolygons(filterB.sharedMesh, filterB.transform));
            WriteDeltaTime("createing nodeB"); 

            nodeA.Invert();
            nodeA.ClipTo(nodeB);
            nodeB.ClipTo(nodeA);
            nodeA.Invert();
            nodeB.Invert();
            WriteDeltaTime("perform subtraction"); 

            currentPolys = nodeA.AllPolygons();
            currentPolys.AddRange(nodeB.AllPolygons());
            WriteDeltaTime("adding to list"); 

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


    [ContextMenu("Subtract Multiple (Optimized)")]
    public void DoMultipleSubtract2()
    {
        if (meshFilterA == null) return;

        // Stap 1: Bouw de hoofd-boom één keer op
        CSGNode mainNode = new CSGNode(MeshToPolygons(meshFilterA.sharedMesh, meshFilterA.transform));

        foreach (var filterB in extraFilters)
        {
            if (filterB == null) continue;

            // Stap 2: Bouw de "brush" (het object dat we eraf trekken)
            CSGNode brush = new CSGNode(MeshToPolygons(filterB.sharedMesh, filterB.transform));

            // Stap 3: Voer de operatie uit op de bestaande boom
            // Dit is de "In-Place" variant van Subtract
            ExecuteSubtract(mainNode, brush);
        }

        // Stap 4: Pas aan het einde één keer converteren naar Mesh
        GetComponent<MeshFilter>().mesh = PolygonsToMesh(mainNode.AllPolygons());
    }

    [ContextMenu("Union Multiple (Optimized)")]
    public void DoMultipleUnion2()
    {
        if (meshFilterA == null) return;

        CSGNode mainNode = new CSGNode(MeshToPolygons(meshFilterA.sharedMesh, meshFilterA.transform));

        foreach (var filterB in extraFilters)
        {
            if (filterB == null) continue;

            CSGNode brush = new CSGNode(MeshToPolygons(filterB.sharedMesh, filterB.transform));
            ExecuteUnion(mainNode, brush);
        }

        GetComponent<MeshFilter>().mesh = PolygonsToMesh(mainNode.AllPolygons());
    }

    // Interne logica die de boom-integriteit bewaart
    private void ExecuteSubtract(CSGNode a, CSGNode b)
    {
        a.Invert();
        a.ClipTo(b);
        b.ClipTo(a);
        b.Invert();
        b.ClipTo(a);
        b.Invert();
        
        // Voeg de overgebleven polygonen van b toe aan de bestaande boom van a
        a.Build(b.AllPolygons());
        a.Invert();
    }

    private void ExecuteUnion(CSGNode a, CSGNode b)
    {
        a.ClipTo(b);
        b.ClipTo(a);
        b.Invert();
        b.ClipTo(a);
        b.Invert();
        
        // Voeg toe aan bestaande boom
        a.Build(b.AllPolygons());
    }    
}