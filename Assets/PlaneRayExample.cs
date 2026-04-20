using UnityEngine;

public class BoxCreator : MonoBehaviour
{
    public enum State { Hover, DrawingBase, DrawingHeight }
    public State currentState = State.Hover;

    public GameObject boxPrefab;
    public LayerMask clickableLayer;

    private GameObject currentBox;
    private Vector3 startPoint;
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
                currentBox.transform.up = normal; // Box uitlijnen met oppervlak

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
            
            // Positie is het midden tussen start en muis
            currentBox.transform.position = (startPoint + currentPoint) / 2f;

            // Bereken schaal op basis van lokale assen
            Vector3 localDiff = currentPoint - startPoint;
            currentBox.transform.localScale = new Vector3(localDiff.x * 1, 0.1f, localDiff.z * 1);
        }

        if (Input.GetMouseButtonUp(0))
        {
            // Zet de dragPlane "overeind" voor de hoogte fase.
            // We gebruiken de forward van de camera, maar projecteren die op de basis van de box
            // zodat het vlak perfect verticaal staat ten opzichte van de box.
            Vector3 planeNormal = Vector3.Cross(currentBox.transform.right, currentBox.transform.up);
            dragPlane = new Plane(planeNormal, currentBox.transform.position);
            
            currentState = State.DrawingHeight;
        }
    }

    void HandleDrawingHeight()
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