using System;
using SP;
using UnityEngine;
using UnityEngine.PlayerLoop;

[DisallowMultipleComponent]
public class Graph : MonoBehaviour
{
    public bool IsFlowReady { get; private set; }

    private void Awake()
    {
        Begin();
    }

    public void Begin()
    {
        // Bootstrap phase: block Service.Enter while graph topology is being reset.
        IsFlowReady = false;

        // 1) Reset all first-level nodes and all descendants to inactive.
        foreach (Transform child in transform)
        {
            var obj = child.gameObject.GetComponent<Service>();
            if (obj != null)
            {
                obj.CloseAllMono();
            }
            SetChildrenActive(child, false);
        }

        // 2) Open the gate, then activate only first-level nodes.
        IsFlowReady = true;

        foreach (Transform child in transform)
        {
            var obj = child.gameObject.GetComponent<Service>();
            if (obj != null)
            {
                obj.OpenAllMono();
            }
        }
    }

    private void SetChildrenActive(Transform parent, bool active)
    {
        foreach (Transform child in parent)
        {
            var obj = child.gameObject.GetComponent<Service>();
            if (obj != null)
            {
                if (active)
                {
                    obj.OpenAllMono();
                }
                else
                {
                    obj.CloseAllMono();
                }
            }

            SetChildrenActive(child, active);
        }
    }
#if UNITY_EDITOR
    public GameObject CreateNodeObject()
    {
        GameObject obj = new GameObject("Node");
        obj.transform.SetParent(transform, false);
        return obj;
    }
#endif
}

