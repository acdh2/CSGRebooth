//===================================================================================================
//                                          OPTIMIZED 1+2 (COPY)
//===================================================================================================
using System.Collections.Generic;
using UnityEngine;
using System;

public class CSGNode
{
    public List<CSGPolygon> polygons = new List<CSGPolygon>();
    public Planed partition;
    public CSGNode front;
    public CSGNode back;
    public Bounds nodeBounds; // De bounds van deze node + kinderen

    private static readonly List<CSGPolygon> _fCopBuffer = new List<CSGPolygon>(64);
    private static readonly List<CSGPolygon> _bCopBuffer = new List<CSGPolygon>(64);

    public CSGNode(List<CSGPolygon> list = null)
    {
        if (list != null && list.Count > 0) Build(list);
    }

    public void Build(List<CSGPolygon> list)
    {
        if (list == null || list.Count == 0) return;

        // 1. Bereken Bounds voor deze node
        this.nodeBounds = CalculateBounds(list);

        // 2. Splitter selectie (Random voor snelheid bij grote lijsten)
        CSGPolygon splitter;
        if (list.Count > 20) 
        {
            splitter = list[UnityEngine.Random.Range(0, list.Count)];
        }
        else 
        {
            splitter = FindBestSplitter(list);
        }

        this.partition = splitter.plane;
        
        List<CSGPolygon> fList = new List<CSGPolygon>();
        List<CSGPolygon> bList = new List<CSGPolygon>();

        for (int i = 0; i < list.Count; i++)
        {
            list[i].Split(this.partition, fList, bList, this.polygons, this.polygons);
        }

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

public void ClipPolygons(List<CSGPolygon> input, List<CSGPolygon> output, bool keepOutside = true)
{
    if (input.Count == 0) return;

    Bounds inputBounds = CalculateBounds(input);

    // 1. Early exit als de bounds de node niet raken
    if (!this.nodeBounds.Intersects(inputBounds))
    {
        float dist = partition.GetDistanceToPoint(input[0].vertices[0].position);
        
        if (dist > CSGConfig.Epsilon) 
        {
            if (this.front != null) this.front.ClipPolygons(input, output, keepOutside);
            else if (keepOutside) 
            {
                LogIfOutsideShape0(input, "Front-Leaf (No Intersect)");
                output.AddRange(input);
            }
        }
        else 
        {
            if (this.back != null) this.back.ClipPolygons(input, output, keepOutside);
        }
        return;
    }

    if (this.partition.normal == Vector3d.zero)
    {
        output.AddRange(input);
        return;
    }

    // 2. Splitting logica
    List<CSGPolygon> f = new List<CSGPolygon>();
    List<CSGPolygon> b = new List<CSGPolygon>();

    foreach (var poly in input)
    {
        _fCopBuffer.Clear();
        _bCopBuffer.Clear();
        poly.Split(this.partition, f, b, _fCopBuffer, _bCopBuffer);
        f.AddRange(_fCopBuffer);
        b.AddRange(_bCopBuffer);
    }

    // 3. Afhandeling van fragmenten (Front)
    if (this.front != null) 
    {
        this.front.ClipPolygons(f, output, keepOutside); // Geef keepOutside door!
    }
    else if (keepOutside) 
    {
        LogIfOutsideShape0(f, "Front-Leaf (After Split)");
        output.AddRange(f);
    }

    // 4. Afhandeling van fragmenten (Back)
    if (this.back != null) 
    {
        this.back.ClipPolygons(b, output, keepOutside); // Geef keepOutside door!
    }
    // In een back-leaf (solid space) wordt b altijd verwijderd, dus geen else nodig.
}

// Hulpmethode voor het loggen van "lekken" buiten de 2x2x2 Cube
private void LogIfOutsideShape0(List<CSGPolygon> polys, string context)
{
    foreach (var p in polys)
    {
        bool isOutside = false;
        foreach (var v in p.vertices)
        {
            // Check of vertex buiten de -1 tot 1 range van Shape_0 valt
            if (Math.Abs(v.position.x) > 1.001f || 
                Math.Abs(v.position.y) > 1.001f || 
                Math.Abs(v.position.z) > 1.001f)
            {
                isOutside = true;
                break;
            }
        }

        if (isOutside)
        {
            FileLogger.Log($"[ARTEFACT] Polygoon overleeft in {context}. " +
                           $"Center: {CalculateBounds(new List<CSGPolygon>{p}).center}. " +
                           $"Normal: {p.plane.normal}");
        }
    }
}

    public void ClipTo(CSGNode other)
    {
        // Als de hele boom van 'this' de boom van 'other' niet raakt, direct klaar.
        if (!this.nodeBounds.Intersects(other.nodeBounds)) return;

        List<CSGPolygon> clipped = new List<CSGPolygon>();
        other.ClipPolygons(this.polygons, clipped);
        this.polygons = clipped;

        front?.ClipTo(other);
        back?.ClipTo(other);
    }

    private Bounds CalculateBounds(List<CSGPolygon> list)
    {
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (var poly in list)
        {
            foreach (var v in poly.vertices)
            {
                min.x = Math.Min(min.x, v.position.x);
                min.y = Math.Min(min.y, v.position.y);
                min.z = Math.Min(min.z, v.position.z);
                max.x = Math.Max(max.x, v.position.x);
                max.y = Math.Max(max.y, v.position.y);
                max.z = Math.Max(max.z, v.position.z);
            }
        }
        Bounds b = new Bounds();
        b.SetMinMax(min, max);
        return b;
    }

    private CSGPolygon FindBestSplitter(List<CSGPolygon> list)
    {
        CSGPolygon best = list[0];
        long bestScore = long.MaxValue;

        int sampleCount = Math.Min(list.Count, 30);
        int step = Math.Max(1, list.Count / sampleCount);

        for (int i = 0; i < list.Count; i += step)
        {
            CSGPolygon candidate = list[i];
            int splits = 0, front = 0, back = 0;

            foreach (var p in list)
            {
                var side = candidate.plane.Compare(p);
                if (side == CSGSide.Spanning) splits++;
                else if (side == CSGSide.Front) front++;
                else if (side == CSGSide.Back) back++;
            }

            long score = (splits * 15) + Math.Abs(front - back);
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
                if (splits == 0 && Math.Abs(front - back) < list.Count / 10) break;
            }
        }
        return best;
    }

    public void Invert()
    {
        for (int i = 0; i < polygons.Count; i++) polygons[i].Flip();
        
        partition.normal = -partition.normal;
        partition.distance = -partition.distance;

        front?.Invert();
        back?.Invert();

        var temp = front;
        front = back;
        back = temp;
    }

    public List<CSGPolygon> AllPolygons()
    {
        List<CSGPolygon> list = new List<CSGPolygon>();
        FillPolygonList(list);
        return list;
    }

    private void FillPolygonList(List<CSGPolygon> list)
    {
        list.AddRange(polygons);
        front?.FillPolygonList(list);
        back?.FillPolygonList(list);
    }

    public void Subtract(CSGNode other)
    {
        this.Invert();
        this.ClipTo(other);
        other.ClipTo(this);
        other.Invert();
        other.ClipTo(this);
        other.Invert();
        this.Build(other.AllPolygons());
        this.Invert();
    }

    public void Union(CSGNode other)
    {
        this.ClipTo(other);
        other.ClipTo(this);
        other.Invert();
        other.ClipTo(this);
        other.Invert();
        this.Build(other.AllPolygons());
    }
}

//====================================================================================================
//                                                      OPTIMIZED 3
//====================================================================================================
// using System.Collections.Generic;
// using UnityEngine;
// using System;

// public class CSGNode
// {
//     public List<CSGPolygon> polygons = new List<CSGPolygon>();
//     public Planed partition;
//     public CSGNode front;
//     public CSGNode back;
//     public Bounds nodeBounds;

//     private static readonly List<CSGPolygon> _fCopBuffer = new List<CSGPolygon>(64);
//     private static readonly List<CSGPolygon> _bCopBuffer = new List<CSGPolygon>(64);

//     public CSGNode(List<CSGPolygon> list = null)
//     {
//         if (list != null && list.Count > 0) Build(list);
//     }

//     public void Build(List<CSGPolygon> list)
//     {
//         if (list == null || list.Count == 0) return;

//         this.nodeBounds = CalculateInitialBounds(list);

//         CSGPolygon splitter = (list.Count > 20) 
//             ? list[UnityEngine.Random.Range(0, list.Count)] 
//             : FindBestSplitter(list);

//         this.partition = splitter.plane;
        
//         List<CSGPolygon> fList = new List<CSGPolygon>();
//         List<CSGPolygon> bList = new List<CSGPolygon>();

//         for (int i = 0; i < list.Count; i++)
//         {
//             list[i].Split(this.partition, fList, bList, this.polygons, this.polygons);
//         }

//         if (fList.Count > 0)
//         {
//             if (this.front == null) this.front = new CSGNode();
//             this.front.Build(fList);
//         }

//         if (bList.Count > 0)
//         {
//             if (this.back == null) this.back = new CSGNode();
//             this.back.Build(bList);
//         }
//     }

//     public void InjectPolygons(List<CSGPolygon> newPolys)
//     {
//         if (newPolys == null || newPolys.Count == 0) return;

//         List<CSGPolygon> f = new List<CSGPolygon>();
//         List<CSGPolygon> b = new List<CSGPolygon>();

//         foreach (var poly in newPolys)
//         {
//             poly.Split(this.partition, f, b, this.polygons, this.polygons);
//         }

//         if (f.Count > 0)
//         {
//             if (front == null) front = new CSGNode(f);
//             else front.InjectPolygons(f);
//         }
//         if (b.Count > 0)
//         {
//             if (back == null) back = new CSGNode(b);
//             else back.InjectPolygons(b);
//         }
//         UpdateBoundsRecursive();
//     }

// public void ClipPolygons(List<CSGPolygon> input, List<CSGPolygon> output)
// {
//     if (input.Count == 0) return;

//     // We splitsen de lijst in Front en Back t.o.v. het huidige partition plane
//     List<CSGPolygon> f = new List<CSGPolygon>();
//     List<CSGPolygon> b = new List<CSGPolygon>();

//     for (int i = 0; i < input.Count; i++)
//     {
//         // De buffers voor coplanar polygonen worden hier genegeerd of toegevoegd aan de juiste kant
//         _fCopBuffer.Clear();
//         _bCopBuffer.Clear();
//         input[i].Split(this.partition, f, b, _fCopBuffer, _bCopBuffer);
        
//         // Bij een standaard clip voegen we coplanar toe aan de front/back 
//         // afhankelijk van de normale (dit voorkomt dubbele vlakken)
//         f.AddRange(_fCopBuffer);
//         b.AddRange(_bCopBuffer);
//     }

//     // RECURSIE:
//     // 1. Verwerk de Front-kant
//     if (this.front != null) 
//     {
//         this.front.ClipPolygons(f, output);
//     }
//     else 
//     {
//         // Front leaf == Buiten de mesh. 
//         // Deze polygonen behouden we.
//         output.AddRange(f);
//     }

//     // 2. Verwerk de Back-kant
//     if (this.back != null) 
//     {
//         this.back.ClipPolygons(b, output);
//     }
//     else 
//     {
//         // Back leaf == Binnen de mesh (Solid).
//         // HIER zit je fout: deze polygonen moeten we NIET toevoegen aan de output.
//         // We laten ze hier simpelweg 'sterven'.
//     }
// }

//     public void ClipTo(CSGNode other)
//     {
//         // Alleen de ClipTo mag de Bounds check gebruiken voor snelheid.
//         // Als de hele boom van 'this' de 'other' niet raakt, hoeven we niks te clippen.
//         if (!this.nodeBounds.Intersects(other.nodeBounds)) return;

//         List<CSGPolygon> clipped = new List<CSGPolygon>();
//         other.ClipPolygons(this.polygons, clipped);
//         this.polygons = clipped;

//         front?.ClipTo(other);
//         back?.ClipTo(other);
        
//         UpdateBoundsRecursive();
//     }

//     public void UpdateBoundsRecursive()
//     {
//         float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
//         float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
//         bool hasData = false;

//         if (polygons.Count > 0)
//         {
//             hasData = true;
//             for (int i = 0; i < polygons.Count; i++)
//             {
//                 var verts = polygons[i].vertices;
//                 for (int j = 0; j < verts.Count; j++)
//                 {
//                     Vector3d p = verts[j].position;
//                     if (p.x < minX) minX = p.x; if (p.y < minY) minY = p.y; if (p.z < minZ) minZ = p.z;
//                     if (p.x > maxX) maxX = p.x; if (p.y > maxY) maxY = p.y; if (p.z > maxZ) maxZ = p.z;
//                 }
//             }
//         }

//         if (front != null)
//         {
//             front.UpdateBoundsRecursive();
//             hasData = true;
//             minX = Mathf.Min(minX, front.nodeBounds.min.x); minY = Mathf.Min(minY, front.nodeBounds.min.y); minZ = Mathf.Min(minZ, front.nodeBounds.min.z);
//             maxX = Mathf.Max(maxX, front.nodeBounds.max.x); maxY = Mathf.Max(maxY, front.nodeBounds.max.y); maxZ = Mathf.Max(maxZ, front.nodeBounds.max.z);
//         }

//         if (back != null)
//         {
//             back.UpdateBoundsRecursive();
//             hasData = true;
//             minX = Mathf.Min(minX, back.nodeBounds.min.x); minY = Mathf.Min(minY, back.nodeBounds.min.y); minZ = Mathf.Min(minZ, back.nodeBounds.min.z);
//             maxX = Mathf.Max(maxX, back.nodeBounds.max.x); maxY = Mathf.Max(maxY, back.nodeBounds.max.y); maxZ = Mathf.Max(maxZ, back.nodeBounds.max.z);
//         }

//         if (hasData)
//         {
//             this.nodeBounds.SetMinMax(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
//         }
//     }

//     private Bounds CalculateInitialBounds(List<CSGPolygon> list)
//     {
//         if (list == null || list.Count == 0) return new Bounds();
//         float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
//         float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;

//         for (int i = 0; i < list.Count; i++)
//         {
//             var verts = list[i].vertices;
//             for (int j = 0; j < verts.Count; j++)
//             {
//                 Vector3d pos = verts[j].position;
//                 if (pos.x < minX) minX = pos.x; if (pos.y < minY) minY = pos.y; if (pos.z < minZ) minZ = pos.z;
//                 if (pos.x > maxX) maxX = pos.x; if (pos.y > maxY) maxY = pos.y; if (pos.z > maxZ) maxZ = pos.z;
//             }
//         }
//         Bounds b = new Bounds();
//         b.SetMinMax(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
//         return b;
//     }

//     private CSGPolygon FindBestSplitter(List<CSGPolygon> list)
//     {
//         CSGPolygon best = list[0];
//         long bestScore = long.MaxValue;
//         int sampleCount = Math.Min(list.Count, 30);
//         int step = Math.Max(1, list.Count / sampleCount);

//         for (int i = 0; i < list.Count; i += step)
//         {
//             CSGPolygon candidate = list[i];
//             int splits = 0, front = 0, back = 0;
//             foreach (var p in list)
//             {
//                 var side = candidate.plane.Compare(p);
//                 if (side == CSGSide.Spanning) splits++;
//                 else if (side == CSGSide.Front) front++;
//                 else if (side == CSGSide.Back) back++;
//             }
//             long score = (splits * 15) + Math.Abs(front - back);
//             if (score < bestScore) { bestScore = score; best = candidate; if (splits == 0) break; }
//         }
//         return best;
//     }

//     public void Invert()
//     {
//         for (int i = 0; i < polygons.Count; i++) polygons[i].Flip();
//         partition.normal = -partition.normal;
//         partition.distance = -partition.distance;
//         front?.Invert();
//         back?.Invert();
//         var temp = front; front = back; back = temp;
//     }

//     public List<CSGPolygon> AllPolygons()
//     {
//         List<CSGPolygon> list = new List<CSGPolygon>();
//         FillPolygonList(list);
//         return list;
//     }

//     private void FillPolygonList(List<CSGPolygon> list)
//     {
//         list.AddRange(polygons);
//         front?.FillPolygonList(list);
//         back?.FillPolygonList(list);
//     }

// // In CSGNode.cs
// public void Subtract(CSGNode other)
// {
//     // Sla de originele bounds op van de kubus (this)
//     Bounds safeBounds = this.nodeBounds; 
//     // Maak ze iets groter voor de zekerheid
//     safeBounds.Expand(0.1f); 

//     this.Invert();
//     this.ClipTo(other);
//     other.ClipTo(this);
//     other.Invert();
//     other.ClipTo(this);
//     other.Invert();
    
//     // Filter de 'other' polygonen VOORDAT je ze injecteert
//     List<CSGPolygon> otherPolys = other.AllPolygons();
//     otherPolys.RemoveAll(p => !safeBounds.Intersects(CalculateInitialBounds(new List<CSGPolygon>{p})));

//     this.InjectPolygons(otherPolys);
//     this.Invert();
//     UpdateBoundsRecursive();
// }

//     public void Union(CSGNode other)
//     {
//         this.ClipTo(other);
//         other.ClipTo(this);
//         other.Invert();
//         other.ClipTo(this);
//         other.Invert();
//         this.InjectPolygons(other.AllPolygons());
//         UpdateBoundsRecursive();
//     }
// }

//===================================================================================================
//                                          ORIGINAL
//===================================================================================================
// using System.Collections.Generic;
// using UnityEngine;
// using System;

// public class CSGNode
// {
//     public List<CSGPolygon> polygons = new List<CSGPolygon>();
//     public Planed partition;
//     public CSGNode front;
//     public CSGNode back;

//     // Herbruikbare buffers op klasseniveau om allocaties in loops te voorkomen
//     private static readonly List<CSGPolygon> _fCopBuffer = new List<CSGPolygon>(64);
//     private static readonly List<CSGPolygon> _bCopBuffer = new List<CSGPolygon>(64);

//     public CSGNode(List<CSGPolygon> list = null)
//     {
//         if (list != null && list.Count > 0) Build(list);
//     }

// public void Build(List<CSGPolygon> list)
// {
//     if (list == null || list.Count == 0) return;

//     //CalculateBounds(list);

//     // Kies een splitter
//     CSGPolygon splitter;
//     if (list.Count > 20) 
//     {
//         // Pak een willekeurige index voor snelheid (O(1) keuze)
//         // Dit voorkomt de O(n^2) bottleneck volledig
//         int randomIndex = UnityEngine.Random.Range(0, list.Count);
//         splitter = list[randomIndex];
//     }
//     else 
//     {
//         // Alleen bij hele kleine lijstjes loont het om even te kijken
//         splitter = FindBestSplitter(list);
//     }

//     this.partition = splitter.plane;
    
//     List<CSGPolygon> fList = new List<CSGPolygon>();
//     List<CSGPolygon> bList = new List<CSGPolygon>();

//     // Verdeel de polygonen over de nieuwe takken
//     for (int i = 0; i < list.Count; i++)
//     {
//         // De splitter zelf komt in 'this.polygons' terecht via de fCoplanar/bCoplanar argumenten
//         list[i].Split(this.partition, fList, bList, this.polygons, this.polygons);
//     }

//     // Recursieve bouw van de takken
//     if (fList.Count > 0)
//     {
//         if (this.front == null) this.front = new CSGNode();
//         this.front.Build(fList);
//     }

//     if (bList.Count > 0)
//     {
//         if (this.back == null) this.back = new CSGNode();
//         this.back.Build(bList);
//     }
// }    

//     public void Build2(List<CSGPolygon> list)
//     {
//         if (list == null || list.Count == 0) return;

//         CSGPolygon splitter = FindBestSplitter(list);
//         this.partition = splitter.plane;
        
//         List<CSGPolygon> fList = new List<CSGPolygon>();
//         List<CSGPolygon> bList = new List<CSGPolygon>();

//         foreach (var poly in list)
//         {
//             // We vullen direct de eigen 'polygons' lijst voor coplanar gevallen
//             poly.Split(this.partition, fList, bList, this.polygons, this.polygons);
//         }

//         if (fList.Count > 0)
//         {
//             if (this.front == null) this.front = new CSGNode();
//             this.front.Build(fList);
//         }

//         if (bList.Count > 0)
//         {
//             if (this.back == null) this.back = new CSGNode();
//             this.back.Build(bList);
//         }
//     }

//     // Geoptimaliseerde ClipPolygons: vult een bestaande lijst ipv een nieuwe te returnen
//     public void ClipPolygons(List<CSGPolygon> input, List<CSGPolygon> output)
//     {
//         if (this.partition.normal == Vector3d.zero) // Safety check voor lege nodes
//         {
//             output.AddRange(input);
//             return;
//         }

//         List<CSGPolygon> f = new List<CSGPolygon>();
//         List<CSGPolygon> b = new List<CSGPolygon>();

//         foreach (var poly in input)
//         {
//             _fCopBuffer.Clear();
//             _bCopBuffer.Clear();
//             poly.Split(this.partition, f, b, _fCopBuffer, _bCopBuffer);
            
//             // Coplanar voegen we direct toe aan de juiste kant
//             f.AddRange(_fCopBuffer);
//             b.AddRange(_bCopBuffer);
//         }

//         if (this.front != null) this.front.ClipPolygons(f, output);
//         else output.AddRange(f);

//         if (this.back != null) this.back.ClipPolygons(b, output);
//         // 'else b.Clear()' is niet nodig, we doen gewoon niets met 'b' (deleten)
//     }

//     public void ClipTo(CSGNode other)
//     {
//         List<CSGPolygon> clipped = new List<CSGPolygon>();
//         other.ClipPolygons(this.polygons, clipped);
//         this.polygons = clipped;

//         front?.ClipTo(other);
//         back?.ClipTo(other);
//     }

//     private CSGPolygon FindBestSplitter(List<CSGPolygon> list)
//     {
//         CSGPolygon best = list[0];
//         long bestScore = long.MaxValue;

//         // Iets grotere steekproef, maar betere vroege exit
//         int sampleCount = Math.Min(list.Count, 30);
//         int step = Math.Max(1, list.Count / sampleCount);

//         for (int i = 0; i < list.Count; i += step)
//         {
//             CSGPolygon candidate = list[i];
//             int splits = 0, front = 0, back = 0;

//             foreach (var p in list)
//             {
//                 var side = candidate.plane.Compare(p);
//                 if (side == CSGSide.Spanning) splits++;
//                 else if (side == CSGSide.Front) front++;
//                 else if (side == CSGSide.Back) back++;
//             }

//             long score = (splits * 15) + Math.Abs(front - back);
//             if (score < bestScore)
//             {
//                 bestScore = score;
//                 best = candidate;
//                 // "Good enough" threshold om n^2 te beperken
//                 if (splits == 0 && Math.Abs(front - back) < list.Count / 10) break;
//             }
//         }
//         return best;
//     }

//     public void Invert()
//     {
//         for (int i = 0; i < polygons.Count; i++) polygons[i].Flip();
        
//         partition.normal = -partition.normal;
//         partition.distance = -partition.distance;

//         front?.Invert();
//         back?.Invert();

//         var temp = front;
//         front = back;
//         back = temp;
//     }

//     public List<CSGPolygon> AllPolygons()
//     {
//         List<CSGPolygon> list = new List<CSGPolygon>();
//         FillPolygonList(list);
//         return list;
//     }

//     private void FillPolygonList(List<CSGPolygon> list)
//     {
//         list.AddRange(polygons);
//         front?.FillPolygonList(list);
//         back?.FillPolygonList(list);
//     }

//     public void Subtract(CSGNode other)
//     {
//         this.Invert();
//         this.ClipTo(other);
//         other.ClipTo(this);
//         other.Invert();
//         other.ClipTo(this);
//         other.Invert();
//         this.Build(other.AllPolygons());
//         this.Invert();
//     }

//     public void Union(CSGNode other)
//     {
//         this.ClipTo(other);
//         other.ClipTo(this);
//         other.Invert();
//         other.ClipTo(this);
//         other.Invert();
//         this.Build(other.AllPolygons());
//     }
// }
