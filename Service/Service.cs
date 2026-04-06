using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    public class Service : MonoBehaviour
    {
        [HideInInspector][SerializeField] private string IRefNextID;
        public virtual void Init()
        {
        }

        protected void Awake()
        {
            enabled = false;
            Init();
        }

        public void Next()
        {
            enabled = false;
            
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                var obj = child.GetComponent<Service>();
                if (obj != null)
                {
                    obj.OpenAllMono();
                }
            }

        }

        public void SetServiceActive(bool active)
        {
            enabled = active;
        }
        public void OpenAllMono()
        {
            foreach (var mono in gameObject.GetComponents<Service>())
            {
                if (mono)
                    mono.SetServiceActive(true);
            }
        }
        public void CloseAllMono()
        {
            foreach (var mono in gameObject.GetComponents<Service>())
            {
                if (mono)
                    mono.SetServiceActive(false);
            }
        }
    }
}
