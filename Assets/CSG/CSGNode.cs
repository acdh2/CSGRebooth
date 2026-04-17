using System.Collections.Generic;
using UnityEngine;

public class CSGNode
{
    public List<CSGPolygon> polygons = new List<CSGPolygon>();
    public Plane partition;
    public CSGNode front;
    public CSGNode back;

    public CSGNode(List<CSGPolygon> list = null)
    {
        if (list != null && list.Count > 0)
        {
            Build(list);
        }
    }

    public void Build(List<CSGPolygon> list)
    {
        if (list == null || list.Count == 0) return;

        // 1. Kies een splitter. We pakken de eerste polygoon uit de lijst.
        // De plane van deze polygoon wordt onze 'partitioner'.
        CSGPolygon splitter = list[0];
        this.partition = splitter.plane;
        
        // Voeg de splitter zelf toe aan de lijst van polygonen van deze node.
        // Polygonen die exact op dit vlak liggen (coplanar) komen ook hier.
        this.polygons.Add(splitter);

        List<CSGPolygon> fList = new List<CSGPolygon>();
        List<CSGPolygon> bList = new List<CSGPolygon>();

        // 2. Verdeel de rest van de polygonen over de voorkant, achterkant, of split ze.
        for (int i = 1; i < list.Count; i++)
        {
            // We gebruiken de uitgebreide Split functie die we eerder hebben geschreven.
            // fCoplanar en bCoplanar voegen we toe aan 'this.polygons'.
            list[i].Split(this.partition, fList, bList, this.polygons, this.polygons);
        }

        // 3. Bouw recursief de voorkant van de boom
        if (fList.Count > 0)
        {
            if (this.front == null) this.front = new CSGNode();
            this.front.Build(fList);
        }

        // 4. Bouw recursief de achterkant van de boom
        if (bList.Count > 0)
        {
            if (this.back == null) this.back = new CSGNode();
            this.back.Build(bList);
        }
    }

    // Draait de hele boom om (nodig voor Subtract)
    public void Invert()
    {
        foreach (var poly in polygons) poly.Flip();
        
        partition.normal = -partition.normal;
        partition.distance = -partition.distance;

        front?.Invert();
        back?.Invert();

        // Swap de kinderen
        CSGNode temp = front;
        front = back;
        back = temp;
    }

    // De filter-functie: splitst polygonen tegen deze boom
    public List<CSGPolygon> ClipPolygons(List<CSGPolygon> list)
    {
        if (partition.normal == Vector3.zero) return new List<CSGPolygon>(list);

        List<CSGPolygon> fList = new List<CSGPolygon>();
        List<CSGPolygon> bList = new List<CSGPolygon>();

        foreach (var poly in list)
        {
            poly.Split(partition, fList, bList, fList, bList);
        }

        if (front != null) fList = front.ClipPolygons(fList);
        
        if (back != null) bList = back.ClipPolygons(bList);
        else bList.Clear(); // Alles wat 'back' van een leaf valt is Inside

        fList.AddRange(bList);
        return fList;
    }

    // Haalt alle polygonen uit de boom op als een platte lijst
    public List<CSGPolygon> AllPolygons()
    {
        List<CSGPolygon> list = new List<CSGPolygon>(polygons);
        if (front != null) list.AddRange(front.AllPolygons());
        if (back != null) list.AddRange(back.AllPolygons());
        return list;
    }
}