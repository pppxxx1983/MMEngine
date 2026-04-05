using System;
using SP;
using UnityEngine;
using UnityEngine.PlayerLoop;

[DisallowMultipleComponent]
public class Graph : MonoBehaviour
{
    public ScriptableObject nodePosAsset;

    private void Start()
    {
        Begin();
    }

    public void Begin()
    {
        foreach (Transform child in transform)
        {
            var obj = child.gameObject.GetComponent<Service>();
            if (obj != null)
            {
                obj.CloseAllMono();
            }
        }

        foreach (Transform child in transform)
        {
            var obj = child.gameObject.GetComponent<Service>();
            if (obj != null)
            {
                obj.OpenAllMono();
            }
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
