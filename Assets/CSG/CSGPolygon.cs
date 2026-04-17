using UnityEngine;
using System.Collections.Generic;

public enum VertexSide { On, Front, Back }

public class CSGPolygon
{
    public List<CSGVertex> vertices;
    public Plane plane; // Unity's ingebouwde Plane is ook prima bruikbaar

    public CSGPolygon(List<CSGVertex> vels)
    {
        vertices = vels;
        plane = new Plane(vertices[0].position, vertices[1].position, vertices[2].position);
    }

    public void Flip()
    {
        vertices.Reverse();
        for (int i = 0; i < vertices.Count; i++)
        {
            var v = vertices[i];
            v.normal = -v.normal;
            vertices[i] = v;
        }
        plane.distance = -plane.distance;
        plane.normal = -plane.normal;
    }

    public void Split(Plane splitPlane, List<CSGPolygon> fList, List<CSGPolygon> bList, List<CSGPolygon> fCoplanar, List<CSGPolygon> bCoplanar)
    {
        // 1. Classificeer elke vertex ten opzichte van het vlak
        List<VertexSide> sides = new List<VertexSide>();
        for (int i = 0; i < vertices.Count; i++)
        {
            float dist = splitPlane.GetDistanceToPoint(vertices[i].position);
            if (dist < -CSGConfig.Epsilon) sides.Add(VertexSide.Front);
            else if (dist > CSGConfig.Epsilon) sides.Add(VertexSide.Back);
            else sides.Add(VertexSide.On);
        }

        // 2. Bepaal of de polygoon in zijn geheel ergens heen kan
        bool hasFront = false;
        bool hasBack = false;
        foreach (var side in sides)
        {
            if (side == VertexSide.Front) hasFront = true;
            if (side == VertexSide.Back) hasBack = true;
        }

        // Geval A: Polygoon ligt volledig op het vlak (Coplanar)
        if (!hasFront && !hasBack)
        {
            float dot = Vector3.Dot(this.plane.normal, splitPlane.normal);
            if (dot > 0) fCoplanar.Add(this);
            else bCoplanar.Add(this);
            return;
        }

        // Geval B: Polygoon ligt volledig aan de voorkant
        if (!hasBack)
        {
            fList.Add(this);
            return;
        }

        // Geval C: Polygoon ligt volledig aan de achterkant
        if (!hasFront)
        {
            bList.Add(this);
            return;
        }

        // Geval D: De polygoon wordt doorsneden, splits hem in twee nieuwe polygonen
        List<CSGVertex> fVerts = new List<CSGVertex>();
        List<CSGVertex> bVerts = new List<CSGVertex>();

        for (int i = 0; i < vertices.Count; i++)
        {
            int j = (i + 1) % vertices.Count;
            CSGVertex vi = vertices[i];
            CSGVertex vj = vertices[j];
            VertexSide si = sides[i];
            VertexSide sj = sides[j];

            // Voeg huidige vertex toe aan de juiste lijst(en)
            if (si != VertexSide.Back) fVerts.Add(vi);
            if (si != VertexSide.Front) bVerts.Add(vi);

            // Check of we de edge (i -> j) moeten snijden
            if ((si == VertexSide.Front && sj == VertexSide.Back) || (si == VertexSide.Back && sj == VertexSide.Front))
            {
                float distI = splitPlane.GetDistanceToPoint(vi.position);
                float distJ = splitPlane.GetDistanceToPoint(vj.position);
                
                // Bereken t (0.0 tot 1.0) op de edge
                float t = Mathf.Abs(distI) / (Mathf.Abs(distI) + Mathf.Abs(distJ));
                
                // Interpoleer positie, normal en UV
                CSGVertex intersect = CSGVertex.Lerp(vi, vj, t);
                
                // Snappen om microscopische gaten te voorkomen
                intersect.position = CSGConfig.Snap(intersect.position);

                fVerts.Add(intersect);
                bVerts.Add(intersect);
            }
        }

        // Voeg de nieuwe fragmenten toe als ze valide polygonen zijn
        if (fVerts.Count >= 3) fList.Add(new CSGPolygon(fVerts));
        if (bVerts.Count >= 3) bList.Add(new CSGPolygon(bVerts));
    }

}