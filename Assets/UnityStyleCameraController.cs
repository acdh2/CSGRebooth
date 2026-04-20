using UnityEngine;

public class UnityStyleCameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float shiftMultiplier = 2.5f; // Voor sneller vliegen met Shift

    [Header("Rotation Settings")]
    public float lookSensitivity = 2f;
    
    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        // Initialiseer de rotatie variabelen op basis van de huidige rotatie
        Vector3 rot = transform.localRotation.eulerAngles;
        rotationY = rot.y;
        rotationX = rot.x;
    }

    void Update()
    {
        HandleRotation();
        HandleMovement();
    }

    void HandleRotation()
    {
        // Alleen draaien als de rechtermuisknop wordt ingedrukt
        if (Input.GetMouseButton(1))
        {
            // Cursor verbergen en locken tijdens het draaien (optioneel, zoals Unity)
            Cursor.lockState = CursorLockMode.Locked;

            float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

            rotationY += mouseX;
            rotationX -= mouseY;

            // Voorkom dat de camera over de kop slaat (clamping)
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);

            transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
        }
        else
        {
            // Cursor weer vrijlaten als de knop wordt losgelaten
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void HandleMovement()
    {
        // Alleen bewegen als we de rechtermuisknop vasthouden (vlieg-modus)
        if (Input.GetMouseButton(1))
        {
            float currentSpeed = moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift)) currentSpeed *= shiftMultiplier;

            float moveX = Input.GetAxis("Horizontal"); // A, D
            float moveZ = Input.GetAxis("Vertical");   // W, S
            
            // Ook Q en E voor omlaag en omhoog
            float moveY = 0;
            if (Input.GetKey(KeyCode.E)) moveY = 1;
            if (Input.GetKey(KeyCode.Q)) moveY = -1;

            Vector3 moveDirection = (transform.forward * moveZ) + (transform.right * moveX) + (Vector3.up * moveY);
            
            transform.position += moveDirection * currentSpeed * Time.deltaTime;
        }
    }
}