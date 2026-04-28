using UnityEngine;
using System.Collections.Generic;

public class CSGTest : CSGModel
{
    public MeshFilter meshFilterA;
    public List<MeshFilter> extraFilters;

[ContextMenu("Subtract Multiple")]
public void DoMultipleSubtract()
{
    // Begin met de basis
    List<CSGPolygon> currentPolys = MeshToPolygons(meshFilterA.sharedMesh, meshFilterA.transform, false);

    foreach(var filterB in extraFilters) 
    {
        CSGNode nodeA = new CSGNode(currentPolys);
        CSGNode nodeB = new CSGNode(MeshToPolygons(filterB.sharedMesh, filterB.transform, false));

        // Subtraction (A - B)
        nodeA.Invert();
        nodeA.ClipTo(nodeB);
        nodeB.ClipTo(nodeA);
        nodeA.Invert();
        nodeB.Invert();

        // BELANGRIJK: Update currentPolys voor de volgende ronde
        currentPolys = nodeA.AllPolygons();
        currentPolys.AddRange(nodeB.AllPolygons());
    }        

    // Nu pas omzetten naar mesh
    GetComponent<MeshFilter>().mesh = PolygonsToMesh(currentPolys, false);
}

public void DoMultipleSubtract3()
{
    // Begin met de volledige dolfijn (A)
    List<CSGPolygon> currentPolys = MeshToPolygons(meshFilterA.sharedMesh, meshFilterA.transform, false);
    List<CSGPolygon> outputPolys = new List<CSGPolygon>();

    foreach(var filterB in extraFilters) 
    {
        // // 1. Bouw de bomen voor de huidige stap
        CSGNode nodeA = new CSGNode(currentPolys);
        CSGNode nodeB = new CSGNode(MeshToPolygons(filterB.sharedMesh, filterB.transform, false));

        // // 2. DE SUBTRACT LOGICA (A - B)
        // // Verwijder delen van A die binnen B liggen
        nodeA.Invert();
        nodeA.ClipTo(nodeB);

        // // Verwijder delen van B die buiten A liggen
        nodeB.ClipTo(nodeA);
        nodeA.Invert();
        // // Draai B om zodat de normalen naar binnen wijzen (de wand van het gat)
        nodeB.Invert();

        // // 3. Update currentPolys: dit is nu de dolfijn met één gat extra
        outputPolys.AddRange(nodeA.AllPolygons());
        outputPolys.AddRange(nodeB.AllPolygons());
        // currentPolys.AddRange(nodeB.AllPolygons());

        currentPolys = outputPolys;
        
        // // In de volgende iteratie van de foreach wordt deze 'currentPolys' 
        // // de nieuwe 'nodeA', waar de volgende kubus uit wordt gesneden.
    }        

    GetComponent<MeshFilter>().mesh = PolygonsToMesh(outputPolys, false);
}

    public void DoMultipleSubtract2()
    {
        List<CSGPolygon> currentPolys = MeshToPolygons(meshFilterA.sharedMesh, meshFilterA.transform, false);
        foreach(var filterB in extraFilters) 
        {
            // 1. Maak de bomen
            CSGNode nodeA = new CSGNode(currentPolys);
            CSGNode nodeB = new CSGNode(MeshToPolygons(filterB.sharedMesh, filterB.transform, false));

            nodeA.ClipTo(nodeB);
            nodeB.ClipTo(nodeA);
            nodeB.Invert();

            // 3. Verzamel alleen wat overblijft
            List<CSGPolygon> nextPolys = new List<CSGPolygon>();
            nextPolys.AddRange(nodeA.AllPolygons());
            nextPolys.AddRange(nodeB.AllPolygons());
            
            currentPolys = nextPolys;
        }        
        GetComponent<MeshFilter>().mesh = PolygonsToMesh(currentPolys, true);
    }
    // public void DoMultipleSubtract()
    // {
    //     // Stel je hebt een array van MeshFilters voor de 'snijders'
    //     // Resultaat begint als Mesh A
    //     List<CSGPolygon> currentPolys = MeshToPolygons(meshFilterA.sharedMesh, meshFilterA.transform, false);

    //     foreach(var filterB in extraFilters) 
    //     {
    //         CSGNode nodeA = new CSGNode(currentPolys);
    //         CSGNode nodeB = new CSGNode(MeshToPolygons(filterB.sharedMesh, filterB.transform, false));

    //         nodeB.Invert();
    //         var clippedA = nodeB.ClipPolygons(nodeA.AllPolygons());
    //         var clippedB = nodeA.ClipPolygons(nodeB.AllPolygons());
    //         nodeB.Invert();

    //         currentPolys = new List<CSGPolygon>();
    //         currentPolys.AddRange(clippedA);
    //         currentPolys.AddRange(clippedB);
    //     }

    //     GetComponent<MeshFilter>().mesh = PolygonsToMesh(currentPolys, true);
    // }

    [ContextMenu("Union Multiple")]
    public void DoMultipleUnion()
    {
        // 1. Start met de polygonen van de basisvorm (Mesh A)
        List<CSGPolygon> currentPolys = MeshToPolygons(meshFilterA.sharedMesh, meshFilterA.transform);

        foreach (var filterB in extraFilters)
        {
            if (filterB == null) continue;

            // Maak bomen van de huidige verzameling en het nieuwe object
            CSGNode nodeA = new CSGNode(currentPolys);
            CSGNode nodeB = new CSGNode(MeshToPolygons(filterB.sharedMesh, filterB.transform));

            // STAP 1: Clip huidige vorm tegen het nieuwe object (houdt buitenkant A)
            nodeB.Invert();
            var clippedA = nodeB.ClipPolygons(nodeA.AllPolygons());
            nodeB.Invert(); // Reset B

            // STAP 2: Clip het nieuwe object tegen de huidige vorm (houdt buitenkant B)
            nodeA.Invert();
            var clippedB = nodeA.ClipPolygons(nodeB.AllPolygons());
            nodeA.Invert(); // Reset A

            // Combineer de resultaten voor de volgende ronde
            currentPolys = new List<CSGPolygon>();
            currentPolys.AddRange(clippedA);
            currentPolys.AddRange(clippedB);
        }

        // 2. Update de mesh met het eindresultaat van alle unies
        GetComponent<MeshFilter>().mesh = PolygonsToMesh(currentPolys);
    }

}