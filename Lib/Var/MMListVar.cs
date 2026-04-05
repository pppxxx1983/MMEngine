using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    [System.Serializable]
    public class MMListVar : IMMVarValidatable
    {
        [HideInInspector]
        public string tag = "Input List";

        public InputType type = InputType.Default;

        [ShowIf(nameof(type), (int)InputType.Default)]
        public List<GameObject> objs = new List<GameObject>();

        [ShowIf(nameof(type), (int)InputType.Output)]
        public MonoBehaviour service;

        [ShowIf(nameof(type), (int)InputType.Global)]
        public string global;

        public virtual bool SupportsDefaultInput => true;
        public virtual bool SupportsServiceInput => true;
        public virtual bool SupportsGlobalInput => true;

        public virtual bool IsInputTypeSupported(InputType inputType)
        {
            switch (inputType)
            {
                case InputType.Output:
                    return SupportsServiceInput;
                case InputType.Global:
                    return SupportsGlobalInput;
                default:
                    return SupportsDefaultInput;
            }
        }

        public virtual InputType GetFallbackInputType()
        {
            if (SupportsDefaultInput)
            {
                return InputType.Default;
            }

            if (SupportsServiceInput)
            {
                return InputType.Output;
            }

            if (SupportsGlobalInput)
            {
                return InputType.Global;
            }

            return InputType.Default;
        }

        public virtual InputType GetResolvedInputType()
        {
            return IsInputTypeSupported(type) ? type : GetFallbackInputType();
        }

        public virtual bool TryValidate(System.Type expectedValueType, bool expectsList, out string error)
        {
            if (!expectsList)
            {
                error = "MMListVar cannot validate as a single input.";
                return false;
            }

            return MMVarValidationUtility.TryValidate(this, expectedValueType, out error);
        }

        public virtual bool ValidateAndLog(System.Type expectedValueType, bool expectsList, UnityEngine.Object context = null)
        {
            if (!expectsList)
            {
                Debug.LogError(tag + " validation failed. MMListVar cannot validate as a single input.", context);
                return false;
            }

            return MMVarValidationUtility.ValidateAndLog(this, expectedValueType, context);
        }
    }

    [System.Serializable]
    public class MMListVar<T> : MMListVar where T : UnityEngine.Object
    {
        protected MMListVar(string typeName)
        {
            tag = "Input " + typeName + " List";
        }

        public List<T> Get()
        {
            base.ValidateAndLog(typeof(T), true, null);
            InputType resolvedType = GetResolvedInputType();

            List<T> results = new List<T>();

            if (resolvedType == InputType.Output)
            {
                if (service == null)
                {
                    return results;
                }

                List<T> serviceValues;
                string error;
                if (!OutputUtility.TryGetOutputListValue(service, out serviceValues, out error))
                {
                    return results;
                }

                if (serviceValues != null)
                {
                    results.AddRange(serviceValues);
                }

                return results;
            }

            if (resolvedType == InputType.Global)
            {
                if (GlobalContext.ins == null)
                {
                    return results;
                }

                if (typeof(T) == typeof(GameObject))
                {
                    List<T> globalValues;
                    if (GlobalContext.ins.TryGetListValue(global, out globalValues) && globalValues != null)
                    {
                        return globalValues;
                    }

                    return results;
                }

                List<T> convertedValues;
                if (GlobalContext.ins.TryGetListValue(global, out convertedValues) && convertedValues != null)
                {
                    results.AddRange(convertedValues);
                }

                return results;
            }

            if (objs == null || objs.Count == 0)
            {
                return results;
            }

            int i;
            for (i = 0; i < objs.Count; i++)
            {
                T value = ConvertGameObject(objs[i]);
                if (value != null)
                {
                    results.Add(value);
                }
            }

            return results;
        }

        public bool TryValidate(out string error)
        {
            return TryValidate(typeof(T), true, out error);
        }

        public bool ValidateAndLog(UnityEngine.Object context = null)
        {
            return base.ValidateAndLog(typeof(T), true, context);
        }

        private static T ConvertGameObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return null;
            }

            if (typeof(T) == typeof(GameObject))
            {
                return gameObject as T;
            }

            if (typeof(T) == typeof(Transform))
            {
                return gameObject.transform as T;
            }

            return gameObject.GetComponent(typeof(T)) as T;
        }
    }
}


