using UnityEditor;
using UnityEngine;

// DIT script staat in Assets/Editor
public static class CSGMenuItems
{
    [MenuItem("CSG Tools/Perform Multiple Subtract %#t")] // Cmd + Shift + R
    public static void PerformMultipleSubtract()
    {
        // 1. Pak het geselecteerde object
        GameObject selected = GameObject.Find("CSGDemo");//Selection.activeGameObject;
        selected.transform.position = Vector3.zero;

        if (selected == null) {
            Debug.LogWarning("Selecteer eerst een object met de CSGTest component!");
            return;
        }

        // 2. Zoek de component
        CSGTest csg = selected.GetComponent<CSGTest>();

        if (csg != null) {
            // 3. Voer de operatie uit
            csg.DoMultipleSubtract();
            
            // 4. Markeer als "Dirty" zodat Unity de wijziging onthoudt
            EditorUtility.SetDirty(csg);
            Debug.Log("CSG Subtract succesvol uitgevoerd via sneltoets.");

            selected.transform.position = new Vector3(0, 0, -5);
        } else {
            Debug.LogError("Geselecteerd object heeft geen CSGTest component.");
        }
    }

    [MenuItem("CSG Tools/Perform Multiple Union %#u")] // Cmd + Shift + U op Mac
    public static void PerformMultipleUnion()
    {
        GameObject selected = GameObject.Find("CSGDemo");//Selection.activeGameObject;
        selected.transform.position = Vector3.zero;

        if (selected == null) {
            selected = GameObject.Find("CSGDemo");
        }

        if (selected == null) {
            Debug.LogWarning("Selecteer een object of zorg dat 'CSGDemo' in de scene staat!");
            return;
        }

        CSGTest csg = selected.GetComponent<CSGTest>();

        if (csg != null) {
            // Undo record zodat je de Union ongedaan kunt maken
            Undo.RecordObject(selected.GetComponent<MeshFilter>(), "CSG Multiple Union");

            // Voer de Union uit
            csg.DoMultipleUnion();
            
            EditorUtility.SetDirty(csg);
            Debug.Log("CSG Multiple Union succesvol uitgevoerd.");

            selected.transform.position = new Vector3(0, 0, -5);
        } else {
            Debug.LogError("Het object heeft geen CSGTest component.");
        }
    }    
}