using UnityEngine;
using System.Collections.Generic;

public class GridShapeBuilder : MonoBehaviour
{
    public enum BuildPhase { Idle, Base, Height }

    [Header("Settings")]
    public List<GameObject> prefabs;
    public LayerMask layerFilter;
    public float gridSize = 0.01f;

    private BuildPhase _phase = BuildPhase.Idle;
    private GameObject _currentInstance;
    private Plane _workPlane;
    
    private Vector3 _originPoint;
    private Vector3 _baseNormal;
    private Vector3 _baseRight;
    private Vector3 _baseForward;
    private Vector3 _currentBaseCenter;

    private float _heightOffset;

    void Update()
    {
        HandleInput();
        if (_phase != BuildPhase.Idle) UpdateConstruction();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            Cancel();
            return;
        }

        if (Input.GetMouseButtonDown(0) && _phase == BuildPhase.Idle) StartBasePhase();
        else if (Input.GetMouseButtonUp(0) && _phase == BuildPhase.Base) StartHeightPhase();
        else if (Input.GetMouseButtonDown(0) && _phase == BuildPhase.Height) FinalizeBuild();
    }

    private void StartBasePhase()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerFilter))
        {
            _baseNormal = hit.normal.normalized;
            _originPoint = SnapToGrid(hit.point);

            _baseForward = Vector3.SqrMagnitude(Vector3.Cross(_baseNormal, Vector3.up)) > 0.01f 
                           ? Vector3.Cross(_baseNormal, Vector3.up).normalized 
                           : Vector3.Cross(_baseNormal, Vector3.forward).normalized;
            _baseRight = Vector3.Cross(_baseNormal, _baseForward).normalized;

            _workPlane = new Plane(_baseNormal, _originPoint);
            _currentInstance = Instantiate(prefabs[0], _originPoint, Quaternion.LookRotation(_baseForward, _baseNormal));
            _phase = BuildPhase.Base;
        }
    }

    private void StartHeightPhase()
    {
        _phase = BuildPhase.Height;

        // Pak de huidige muispositie op de nieuwe plane direct bij de start
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 camDir = Camera.main.transform.forward;
        Vector3 planeNormal = Vector3.ProjectOnPlane(camDir, _baseNormal).normalized;
        _workPlane = new Plane(planeNormal, _currentBaseCenter);

        if (_workPlane.Raycast(ray, out float dist))
        {
            Vector3 hitPoint = SnapToGrid(ray.GetPoint(dist));
            // Sla op hoe ver de muis nu al 'boven' de basis zweeft
            _heightOffset = Vector3.Dot(hitPoint - _currentBaseCenter, _baseNormal);
        }
    }

    private void UpdateConstruction()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (_phase == BuildPhase.Base)
        {
            if (!_workPlane.Raycast(ray, out float dist)) return;
            Vector3 hitPoint = SnapToGrid(ray.GetPoint(dist));
            Vector3 diff = hitPoint - _originPoint;

            float sizeRight = Vector3.Dot(diff, _baseRight);
            float sizeForward = Vector3.Dot(diff, _baseForward);

            _currentBaseCenter = _originPoint + (_baseRight * sizeRight * 0.5f) + (_baseForward * sizeForward * 0.5f);
            _currentInstance.transform.position = _currentBaseCenter;
            _currentInstance.transform.localScale = new Vector3(Mathf.Abs(sizeRight), 0.01f, Mathf.Abs(sizeForward));
        }
        else if (_phase == BuildPhase.Height)
        {
            // Richt de plane naar de camera, maar houd hem verticaal t.o.v. het oppervlak
            Vector3 camDir = Camera.main.transform.forward;
            Vector3 planeNormal = Vector3.ProjectOnPlane(camDir, _baseNormal).normalized;
            _workPlane = new Plane(planeNormal, _currentBaseCenter);

            if (!_workPlane.Raycast(ray, out float dist)) return;
            Vector3 hitPoint = SnapToGrid(ray.GetPoint(dist));
            
            // Bereken hoogte langs de oorspronkelijke oppervlak-normaal
            //float height = Vector3.Dot(hitPoint - _currentBaseCenter, _baseNormal);
            float rawHeight = Vector3.Dot(hitPoint - _currentBaseCenter, _baseNormal);
            float height = rawHeight - _heightOffset;
            
            Vector3 s = _currentInstance.transform.localScale;
            _currentInstance.transform.localScale = new Vector3(s.x, Mathf.Abs(height), s.z);
            // Verschuif het middenpunt mee om de basis op zijn plek te houden (voor center-pivot prefabs)
            _currentInstance.transform.position = _currentBaseCenter + (_baseNormal * height * 0.5f);
        }
    }

    private void FinalizeBuild() => _phase = BuildPhase.Idle;

    private void Cancel()
    {
        if (_currentInstance) Destroy(_currentInstance);
        _phase = BuildPhase.Idle;
    }

    private Vector3 SnapToGrid(Vector3 pos) => new Vector3(
        Mathf.Round(pos.x / gridSize) * gridSize,
        Mathf.Round(pos.y / gridSize) * gridSize,
        Mathf.Round(pos.z / gridSize) * gridSize
    );
}