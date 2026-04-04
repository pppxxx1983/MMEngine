using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    public enum ServiceOutputSourceType
    {
        Self,
        Global
    }
    public class Service : MonoBehaviour
    {
        // [Enter] public List<Service> _enters=new List<Service>();
        // [Next]  public List<Service> _next=new List<Service>();
        // [HideInInspector]public List<Service> _inputs = new List<Service>();
        // [HideInInspector]public List<Service> _outputs = new List<Service>();

        [HideInInspector]
        [SerializeField]
        private ServiceOutputSourceType outputSourceType = ServiceOutputSourceType.Self;

        [HideInInspector]
        [SerializeField]
        private string outputGlobalKey;

        public ServiceOutputSourceType OutputSourceType => outputSourceType;

        public string OutputGlobalKey => outputGlobalKey;

        public T GetOutput<T>() where T : UnityEngine.Object
        {
            T value;
            string error;
            if (ServiceOutputUtility.TryGetOutputValue(this, out value, out error))
            {
                return value;
            }

            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError(error, this);
            }

            return null;
        }

        public bool TryGetOutputValue<T>(out T value)
        {
            value = default(T);
            object rawValue;
            string error;
            if (!ServiceOutputUtility.TryGetOutputValue(this, typeof(T), out rawValue, out error))
            {
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError(error, this);
                }

                return false;
            }

            if (rawValue == null)
            {
                return !typeof(T).IsValueType || Nullable.GetUnderlyingType(typeof(T)) != null;
            }

            value = (T)rawValue;
            return true;
        }

        public List<T> GetOutputList<T>() where T : UnityEngine.Object
        {
            List<T> values;
            string error;
            if (ServiceOutputUtility.TryGetOutputListValue(this, out values, out error))
            {
                return values;
            }

            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError(error, this);
            }

            return new List<T>();
        }
        
        public virtual void Init()
        {
        }

        public virtual void Enter()
        {
        }

        public virtual void Exit()
        {
        }

        public virtual void Cancel()
        {
        }

        public void NextService()
        {
            // CloseAllMono();
            // foreach (var mono in gameObject.GetComponents<Service>())
            // {
            //     if (mono)
            //         mono.SetServiceActive(false);
            // }

            var mono = gameObject.GetComponent<Service>();
            mono.enabled = false;
            
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                var obj = child.GetComponent<Service>();
                if (obj != null)
                {
                    obj.OpenAllMono();
                }

                // Only advance one level; deeper descendants stay inactive.
                SetDescendantServicesActive(child, false);
            }
        }

        private static void SetDescendantServicesActive(Transform parent, bool active)
        {
            foreach (Transform child in parent)
            {
                var obj = child.GetComponent<Service>();
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

                SetDescendantServicesActive(child, active);
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
        protected virtual void OnEnable()
        {
            Graph graph = GetComponentInParent<Graph>();
            if (graph != null && !graph.IsFlowReady)
            {
                
                return;
            }
            Enter();
        }

        public virtual void Update()
        {
        }

        public virtual void FixedUpdate()
        {
        }

        public virtual void LateUpdate()
        {
        }
    }
}

