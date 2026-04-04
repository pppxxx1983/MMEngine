using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    
    public enum InputType
    {
        Default,
        Service,
        Global
    }

    [System.Serializable]
    public class MMVar : IMMVarValidatable
    {
        [HideInInspector]
        public string tag = "Input Transform";

        public InputType type = InputType.Default;

        [ShowIf(nameof(type), (int)InputType.Default)]
        public GameObject obj;

        [ShowIf(nameof(type), (int)InputType.Service)]
        public Service service;

        
        [ShowIf(nameof(type), (int)InputType.Global)]
        public string global;

        public virtual bool SupportsDefaultInput => true;
        public virtual bool SupportsServiceInput => true;
        public virtual bool SupportsGlobalInput => true;

        public virtual bool IsInputTypeSupported(InputType inputType)
        {
            switch (inputType)
            {
                case InputType.Service:
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
                return InputType.Service;
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
            if (expectsList)
            {
                error = "Single MMVar cannot validate as a list input.";
                return false;
            }

            return MMVarValidationUtility.TryValidate(this, expectedValueType, out error);
        }

        public virtual bool ValidateAndLog(System.Type expectedValueType, bool expectsList, UnityEngine.Object context = null)
        {
            if (expectsList)
            {
                Debug.LogError(tag + " validation failed. Single MMVar cannot validate as a list input.", context);
                return false;
            }

            return MMVarValidationUtility.ValidateAndLog(this, expectedValueType, context);
        }

    }
    
    
    [System.Serializable]
    public class MMVar<T> : MMVar where T : UnityEngine.Object
    {
        protected MMVar(string typeName)
        {
            tag = "Input " + typeName;
        }

        public T Get()
        {
            base.ValidateAndLog(typeof(T), false, null);
            InputType resolvedType = GetResolvedInputType();

            if (resolvedType == InputType.Service)
            {
                if (service == null)
                {
                    return null;
                }

                return service.GetOutput<T>();
            }

            if (resolvedType == InputType.Global)
            {
                if (GlobalContext.ins == null)
                {
                    return null;
                }

                T globalValue;
                if (GlobalContext.ins.TryGetValue(global, out globalValue))
                {
                    return globalValue;
                }

                return null;
            }

            if (obj == null)
            {
                return null;
            }

            if (typeof(T) == typeof(GameObject))
            {
                return obj as T;
            }

            return obj.GetComponent(typeof(T)) as T;
        }

        public bool TryValidate(out string error)
        {
            return TryValidate(typeof(T), false, out error);
        }

        public bool ValidateAndLog(UnityEngine.Object context = null)
        {
            return base.ValidateAndLog(typeof(T), false, context);
        }
    }
}
