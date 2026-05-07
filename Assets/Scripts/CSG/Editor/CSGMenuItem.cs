using UnityEditor;
using UnityEngine;

public static class CSGMenuItems
{
    /** 
     * Shortcut: Cmd/Ctrl + Shift + T 
     * Calls the ExecuteStack method on all CSGExample objects in the scene.
     */
    [MenuItem("CSG/Perform all CSG %#t")]
    public static void PerformAllCSG()
    {
        CSGExample[] targets = Object.FindObjectsByType<CSGExample>(FindObjectsSortMode.None);

        if (targets.Length == 0)
        {
            Debug.LogWarning("No CSGExample components found in the scene.");
            return;
        }

        Undo.RecordObjects(targets, "Execute CSG Stack");

        foreach (CSGExample target in targets)
        {
            target.ExecuteStack();
            // Mark the object as "dirty" to notify Unity the mesh has changed
            EditorUtility.SetDirty(target);
        }
    }
}