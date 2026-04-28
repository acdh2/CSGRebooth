using System.Collections.Generic;
using UnityEngine;
using System;


public class CSGNode
{
    public List<CSGPolygon> polygons = new List<CSGPolygon>();
    public Planed partition;
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

    // 1. Zoek de beste splitter in plaats van altijd list[0]
    CSGPolygon splitter = FindBestSplitter(list);
    this.partition = splitter.plane;
    
    List<CSGPolygon> fList = new List<CSGPolygon>();
    List<CSGPolygon> bList = new List<CSGPolygon>();

    // 2. Verdeel de polygonen
    foreach (var poly in list)
    {
        // De splitter zelf en andere coplanar polygonen gaan naar this.polygons
        poly.Split(this.partition, fList, bList, this.polygons, this.polygons);
    }

    // 3. Bouw recursief verder
    if (fList.Count > 0)
    {
        if (this.front == null) this.front = new CSGNode();
        this.front.Build(fList);
    }

    if (bList.Count > 0)
    {
        if (this.back == null) this.back = new CSGNode();
        this.back.Build(bList);
    }
}

    private CSGPolygon FindBestSplitter(List<CSGPolygon> list)
    {
        CSGPolygon best = list[0];
        long bestScore = long.MaxValue;

        // We testen een steekproef om de snelheid erin te houden
        int sampleStep = Math.Max(1, list.Count / 15); 

        for (int i = 0; i < list.Count; i += sampleStep)
        {
            CSGPolygon candidate = list[i];
            int splits = 0;
            int front = 0;
            int back = 0;

            foreach (var p in list)
            {
                var side = candidate.plane.Compare(p); // Gebruik je bestaande Side-check
                if (side == CSGSide.Spanning) splits++;
                else if (side == CSGSide.Front) front++;
                else if (side == CSGSide.Back) back++;
            }

            // Score: We haten splits (wegens precisiefouten) en willen balans
            long score = (splits * 10) + Math.Abs(front - back);

            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }
        return best;
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

    public List<CSGPolygon> ClipPolygonsNew(List<CSGPolygon> list)
    {
        //if (this.partition == null) return new List<CSGPolygon>(list);

        List<CSGPolygon> f = new List<CSGPolygon>();
        List<CSGPolygon> b = new List<CSGPolygon>();

        foreach (var poly in list)
        {
            // Gebruik je bestaande Split-methode om te verdelen
            // We gebruiken hier tijdelijke lijsten voor de coplanar resultaten
            List<CSGPolygon> fCop = new List<CSGPolygon>();
            List<CSGPolygon> bCop = new List<CSGPolygon>();
            
            poly.Split(this.partition, f, b, fCop, bCop);

            // CRUCIAL: Routeer coplanar polygonen naar Front of Back
            // In een Subtract operatie wil je dat 'gelijke' vlakken 
            // als buiten (Front) worden gezien om gaten te voorkomen.
            f.AddRange(fCop);
            b.AddRange(bCop);
        }

        if (this.front != null) f = this.front.ClipPolygons(f);
        
        if (this.back != null) b = this.back.ClipPolygons(b);
        else {
            // List<CSGPolygon> keepList = new List<CSGPolygon>();
            // foreach (var poly in b) {
            //     if (poly.GetArea() < 0.0001)
            //     {
            //         keepList.Add(poly);
            //     }
            // }
            b.Clear(); // Hier verdwijnt je grote driehoek als hij in 'b' belandt!
        }

        f.AddRange(b);
        return f;
    }    

    public void ClipTo(CSGNode other)
    {
        // De polygonen van deze node worden vervangen door 
        // de versie die is 'geclipt' door de andere boom.
        this.polygons = other.ClipPolygons(this.polygons);

        if (this.front != null) this.front.ClipTo(other);
        if (this.back != null) this.back.ClipTo(other);
    }    

public List<CSGPolygon> ClipPolygons(List<CSGPolygon> list)
{
    List<CSGPolygon> f = new List<CSGPolygon>();
    List<CSGPolygon> b = new List<CSGPolygon>();

    foreach (var poly in list)
    {
        // Gebruik aparte lijsten voor coplanar!
        List<CSGPolygon> fCop = new List<CSGPolygon>();
        List<CSGPolygon> bCop = new List<CSGPolygon>();
        poly.Split(this.partition, f, b, fCop, bCop);

        // Bij een subtract/union moeten coplanar vlakken naar Front 
        // om dubbele muren en gaten te voorkomen.
        f.AddRange(fCop);
        f.AddRange(bCop); 
    }

    if (this.front != null) f = this.front.ClipPolygons(f);
    if (this.back != null) b = this.back.ClipPolygons(b);
    else b.Clear(); // Dit mag ALLEEN als je zeker weet dat 'null' Solid is.

    f.AddRange(b);
    return f;
}    

    // De filter-functie: splitst polygonen tegen deze boom
    public List<CSGPolygon> ClipPolygons2(List<CSGPolygon> list)
    {
        if (partition.normal == Vector3d.zero) return new List<CSGPolygon>(list);

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