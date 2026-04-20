using UnityEngine;

public class BoxCreator : MonoBehaviour
{
    public enum State { Hover, DrawingBase, DrawingHeight }
    public State currentState = State.Hover;

    public GameObject boxPrefab;
    public LayerMask clickableLayer;

    private GameObject currentBox;
    private Vector3 startPoint;
    private Vector3 endPoint;
    private Plane dragPlane;

    void Update()
    {
        switch (currentState)
        {
            case State.Hover:
                HandleHover();
                break;
            case State.DrawingBase:
                HandleDrawingBase();
                break;
            case State.DrawingHeight:
                HandleDrawingHeight();
                break;
        }
    }

void UpdateBoxTransform(Vector3 pA, Vector3 pB)
{
    if (currentBox == null) return;

    // 1. Center en Scale berekenen
    Vector3 center = (pA + pB) / 2f;
    currentBox.transform.position = center;

    float sizeX = Mathf.Max(Mathf.Abs(pA.x - pB.x), 0.01f);
    float sizeY = Mathf.Max(Mathf.Abs(pA.y - pB.y), 0.01f);
    float sizeZ = Mathf.Max(Mathf.Abs(pA.z - pB.z), 0.01f);

    Vector3 newScale = new Vector3(sizeX, sizeY, sizeZ);
    currentBox.transform.localScale = newScale;

    // 2. Planar UV mapping toepassen
    ApplyWorldPlanarUVs(currentBox);
}

void ApplyWorldPlanarUVs(GameObject obj)
{
    MeshFilter mf = obj.GetComponent<MeshFilter>();
    if (mf == null) return;

    Mesh mesh = mf.mesh; 
    Vector2[] uvs = mesh.uv;
    Vector3[] normals = mesh.normals;
    Vector3[] vertices = mesh.vertices;

    for (int i = 0; i < uvs.Length; i++)
    {
        // 1. Bereken de wereldpositie van deze specifieke vertex
        Vector3 worldPos = obj.transform.TransformPoint(vertices[i]);
        
        // 2. Bereken de wereld-richting van de normal
        Vector3 worldNormal = obj.transform.TransformDirection(normals[i]);

        float absX = Mathf.Abs(worldNormal.x);
        float absY = Mathf.Abs(worldNormal.y);
        float absZ = Mathf.Abs(worldNormal.z);

        // 3. Projecteer de wereldpositie op het dominante vlak
        if (absX >= absY && absX >= absZ) // Vlak wijst hoofdzakelijk naar links/rechts (X)
        {
            // We gebruiken Z en Y van de wereldpositie
            uvs[i] = new Vector2(worldPos.z, worldPos.y);
        }
        else if (absY >= absX && absY >= absZ) // Vlak wijst hoofdzakelijk omhoog/omlaag (Y)
        {
            // We gebruiken X en Z van de wereldpositie
            uvs[i] = new Vector2(worldPos.x, worldPos.z);
        }
        else // Vlak wijst hoofdzakelijk naar voren/achteren (Z)
        {
            // We gebruiken X en Y van de wereldpositie
            uvs[i] = new Vector2(worldPos.x, worldPos.y);
        }
    }

    mesh.uv = uvs;
}

    void UpdateBoxTransform2(Vector3 pA, Vector3 pB)
    {
        if (currentBox == null) return;

        // 1. Bereken het midden (Center)
        Vector3 center = (pA + pB) / 2f;
        currentBox.transform.position = center;

        // 2. Bereken de grootte (Size)
        // We gebruiken Abs om negatieve schaal (en dus rendering fouten) te voorkomen
        float sizeX = Mathf.Abs(pA.x - pB.x);
        float sizeY = Mathf.Abs(pA.y - pB.y);
        float sizeZ = Mathf.Abs(pA.z - pB.z);

        // 3. Pas de schaal toe
        // Let op: als een waarde 0 is (bijv. bij de start), 
        // geef het een kleine waarde om Unity-waarschuwingen te voorkomen.
        currentBox.transform.localScale = new Vector3(
            Mathf.Max(sizeX, 0.01f), 
            Mathf.Max(sizeY, 0.01f), 
            Mathf.Max(sizeZ, 0.01f)
        );
    }    

    Ray GetMouseRay() {
        return Camera.main.ScreenPointToRay(Input.mousePosition);
    }


    void HandleHover()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(GetMouseRay(), out RaycastHit hit, Mathf.Infinity, clickableLayer))
            {
                startPoint = hit.point;
                Vector3 normal = hit.normal;
                dragPlane = new Plane(normal, startPoint);

                currentBox = Instantiate(boxPrefab, startPoint, Quaternion.identity);
                currentBox.transform.localScale = Vector3.zero;
                //currentBox.transform.up = normal; // Box uitlijnen met oppervlak

                currentState = State.DrawingBase;
            }
        }
    }

    void HandleDrawingBase()
    {
        Ray ray = GetMouseRay();
        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 currentPoint = ray.GetPoint(enter);
            endPoint = currentPoint;
            UpdateBoxTransform(startPoint, currentPoint);            
        }

        if (Input.GetMouseButtonUp(0))
        {            
            currentState = State.DrawingHeight;

        }
    }

void HandleDrawingHeight()
{
    Ray ray = GetMouseRay();

    // 1. Maak een vlak dat verticaal staat (up) en naar de camera kijkt
    Vector3 planeNormal = Camera.main.transform.forward;

    // Het vlak gaat door het startPoint (of het midden van je base)
    Plane heightPlane = new Plane(-planeNormal, endPoint);
    
    if (heightPlane.Raycast(ray, out float enter))
    {
        Vector3 hitPoint = ray.GetPoint(enter);
        float height = Vector3.Dot(dragPlane.normal, hitPoint - endPoint);
        UpdateBoxTransform(startPoint, endPoint + dragPlane.normal * height);
    }

    if (Input.GetMouseButtonDown(0))
    {
        currentBox.layer = 6;
        currentState = State.Hover;
        currentBox = null;
    }
}    

    void HandleDrawingHeight2()
    {
        Ray ray = GetMouseRay();
        
        // We berekenen hoe ver de ray verwijderd is van het vlak
        // Dit geeft ons de verticale afstand (hoogte) ongeacht de camerahoek
        double height = dragPlane.GetDistanceToPoint(ray.origin + ray.direction * (Vector3.Dot(startPoint - ray.origin, dragPlane.normal) / 
                        Vector3.Dot(ray.direction, dragPlane.normal)));

        // Alternatieve simpele methode: afstand van ray tot het vlak
        // We gebruiken een tijdelijke double om de enter distance te krijgen
        if (dragPlane.Raycast(ray, out float enter))
        {
            // We willen niet het punt OP het vlak, maar de afstand van de ray origin tot het vlak.
            // Een robuuste manier is de afstand tussen het basispunt en het dichtstbijzijnde punt op de ray.
            Vector3 pointOnPlane = ray.GetPoint(enter);
            double dist = Vector3.Distance(ray.origin, pointOnPlane); // Dit is niet ideaal bij hoeken.
        }

        // De meest betrouwbare manier voor 'scherm-naar-hoogte':
        // Gebruik de muisbeweging op de Y-as of een tweede raycast.
        
        // Verbeterde versie van HandleDrawingHeight:
        Plane heightPlane = new Plane(Camera.main.transform.forward, currentBox.transform.position);
        if (heightPlane.Raycast(ray, out float distance))
        {
            Vector3 currentPoint = ray.GetPoint(distance);
            // Projecteer de afstand op de 'up' vector van de box
            float currentHeight = Vector3.Dot(currentPoint - currentBox.transform.position, currentBox.transform.up);
            
            // Update de schaal (x en z blijven gelijk aan de basis)
            Vector3 s = currentBox.transform.localScale;
            currentBox.transform.localScale = new Vector3(s.x, currentHeight * 2f, s.z);

            // Verplaats de positie zodat de onderkant op de grond blijft
            // De box groeit van nature vanuit het midden, dus we moeten hem 'half' omhoog duwen
            Vector3 basePosition = (startPoint + (startPoint + (currentBox.transform.right * s.x) + (currentBox.transform.forward * s.z))) / 2f; // Dit is complexer.
            
            // Makkelijker: Gebruik een container/pivot of pas de center aan:
            // Voor nu houden we de positie-offset simpel:
            currentBox.transform.position = ((startPoint + (ray.GetPoint(enter))) / 2f) + (currentBox.transform.up * currentHeight / 2f);
        }

        if (Input.GetMouseButtonDown(0))
        {
            currentState = State.Hover;
            currentBox = null;
        }
    }
}