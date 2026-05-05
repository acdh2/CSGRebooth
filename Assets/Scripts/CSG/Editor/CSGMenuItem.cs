using UnityEditor;
using UnityEngine;
using System.Diagnostics;

/**
 * Provides editor menu items to trigger Constructive Solid Geometry (CSG) operations 
 * directly from the Unity menu or via keyboard shortcuts.
 */
public static class CSGMenuItems
{
    /** 
     * Shortcut: Cmd/Ctrl + Shift + T 
     * Finds the 'CSGDemo' object and executes a multiple subtraction operation with timing.
     */
    [MenuItem("CSG Tools/Perform Multiple Subtract %#t")]
    public static void PerformMultipleSubtract()
    {
        GameObject selected = GameObject.Find("CSGDemo");
        if (selected == null) {
            UnityEngine.Debug.LogWarning("Object 'CSGDemo' not found in the scene!");
            return;
        }
        
        selected.transform.position = Vector3.zero;
        CSGTest csg = selected.GetComponent<CSGTest>();

        if (csg != null) {
            // Start timing
            Stopwatch sw = Stopwatch.StartNew();
            long startTime = sw.ElapsedMilliseconds;

            csg.DoMultipleSubtract();
            
            sw.Stop();
            long endTime = sw.ElapsedMilliseconds;
            long deltaTime = endTime - startTime;

            EditorUtility.SetDirty(csg);
            UnityEngine.Debug.Log($"CSG Subtract completed. Delta Time: {deltaTime}ms. Total Elapsed: {sw.ElapsedMilliseconds}ms.");

            selected.transform.position = new Vector3(0, 0, -5);
        } else {
            UnityEngine.Debug.LogError("The selected object does not have a CSGTest component.");
        }
    }

    /** 
     * Shortcut: Cmd/Ctrl + Shift + U 
     * Finds the 'CSGDemo' object and executes a multiple union operation with timing.
     */
    [MenuItem("CSG Tools/Perform Multiple Union %#u")]
    public static void PerformMultipleUnion()
    {
        GameObject selected = GameObject.Find("CSGDemo");
        if (selected == null) {
            UnityEngine.Debug.LogWarning("Object 'CSGDemo' not found in the scene!");
            return;
        }

        selected.transform.position = Vector3.zero;
        CSGTest csg = selected.GetComponent<CSGTest>();

        if (csg != null) {
            Undo.RecordObject(selected.GetComponent<MeshFilter>(), "CSG Multiple Union");

            // Start timing
            Stopwatch sw = Stopwatch.StartNew();
            long startTime = sw.ElapsedMilliseconds;

            csg.DoMultipleUnion();
            
            sw.Stop();
            long endTime = sw.ElapsedMilliseconds;
            long deltaTime = endTime - startTime;
            
            EditorUtility.SetDirty(csg);
            UnityEngine.Debug.Log($"CSG Multiple Union completed. Delta Time: {deltaTime}ms. Total Elapsed: {sw.ElapsedMilliseconds}ms.");

            selected.transform.position = new Vector3(0, 0, -5);
        } else {
            UnityEngine.Debug.LogError("The selected object does not have a CSGTest component.");
        }
    }    
}