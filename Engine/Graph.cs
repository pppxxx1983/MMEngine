using System;
using System.Collections.Generic;
using SP;
using UnityEngine;
using UnityEngine.PlayerLoop;

[DisallowMultipleComponent]
public class Graph : MonoBehaviour
{
    public ScriptableObject nodePosAsset;
    [SerializeField] private List<Transform> groupParents = new List<Transform>();

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

    public void RegisterGroupParent(Transform groupParent)
    {
        if (groupParent == null)
        {
            return;
        }

        if (groupParents == null)
        {
            groupParents = new List<Transform>();
        }

        for (int i = groupParents.Count - 1; i >= 0; i--)
        {
            if (groupParents[i] == null)
            {
                groupParents.RemoveAt(i);
                continue;
            }

            if (groupParents[i] == groupParent)
            {
                return;
            }
        }

        groupParents.Add(groupParent);
    }
#endif

    public IReadOnlyList<Transform> GroupParents => groupParents;
}
