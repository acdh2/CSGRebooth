using UnityEngine;
using System.Collections.Generic;

public class CSGTest : CSGModel
{
    public MeshFilter meshFilterA;
    public List<MeshFilter> extraFilters;

    [ContextMenu("Subtract Multiple")]
    public void DoMultipleSubtract()
    {
        // Stel je hebt een array van MeshFilters voor de 'snijders'
        // Resultaat begint als Mesh A
        List<CSGPolygon> currentPolys = MeshToPolygons(meshFilterA.sharedMesh, meshFilterA.transform, false);

        foreach(var filterB in extraFilters) 
        {
            CSGNode nodeA = new CSGNode(currentPolys);
            CSGNode nodeB = new CSGNode(MeshToPolygons(filterB.sharedMesh, filterB.transform));

            nodeB.Invert();
            var clippedA = nodeB.ClipPolygons(nodeA.AllPolygons());
            var clippedB = nodeA.ClipPolygons(nodeB.AllPolygons());

            currentPolys = new List<CSGPolygon>();
            currentPolys.AddRange(clippedA);
            currentPolys.AddRange(clippedB);
        }

        GetComponent<MeshFilter>().mesh = PolygonsToMesh(currentPolys, true);
    }

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