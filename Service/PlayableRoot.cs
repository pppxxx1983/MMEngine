using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-10000)]
public class Root : MonoBehaviour
{
    public static Root Instance { get; private set; }
    public ResourceCenter resourceCenter;
    public GlobalAudioManager audioManager;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

}