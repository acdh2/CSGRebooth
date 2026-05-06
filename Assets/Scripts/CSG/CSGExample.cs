using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;

public class CSGExample : MonoBehaviour
{
    [System.Serializable]
    public struct CSGAction
    {
        public CSGType type;
        public MeshFilter filter;
    }

    public List<CSGAction> actions;

    [ContextMenu("Execute Actions")]
    public void ExecuteStack()
    {        
        if (actions == null || actions.Count == 0) return;

        transform.position = Vector3.zero;

        Stopwatch sw = Stopwatch.StartNew();

        CSGStack stack = new CSGStack();

        foreach (var action in actions)
        {
            if (action.filter == null || action.filter.sharedMesh == null) continue;
            
            stack.AddOperation(
                action.type, 
                action.filter.sharedMesh, 
                action.filter.transform.localToWorldMatrix
            );
        }

        MeshFilter targetFilter = GetComponent<MeshFilter>();
        if (targetFilter != null)
        {
            targetFilter.mesh = stack.GetMesh(this.transform, true);
        }

        sw.Stop();
        UnityEngine.Debug.Log($"CSG Stack executed in {sw.ElapsedMilliseconds} ms ({sw.Elapsed.TotalSeconds:F4} seconds)");

        transform.position = new Vector3(0, 0, -5);

    }
}
