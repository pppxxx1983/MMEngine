using UnityEngine;

namespace SP
{
    [System.Serializable]
    public class Vector3Var : MMVar
    {
        [ShowIf(nameof(type), (int)InputType.Default)]
        public Vector3 value = Vector3.zero;

        public Vector3Var()
        {
            tag = "Input Vector3";
        }

        public Vector3 Get()
        {
            base.ValidateAndLog(typeof(Vector3), false, null);

            if (type == InputType.Output)
            {
                if (service == null)
                {
                    return value;
                }

                object rawValue;
                string error;
                if (OutputUtility.TryGetOutputValue(service, typeof(Vector3), out rawValue, out error) && rawValue is Vector3 resolvedValue)
                {
                    return resolvedValue;
                }

                return value;
            }

            if (type == InputType.Global)
            {
                if (GlobalContext.ins == null || string.IsNullOrEmpty(global))
                {
                    return value;
                }

                Vector3 globalValue;
                if (GlobalContext.ins.TryGetValue(global, out globalValue))
                {
                    return globalValue;
                }

                return value;
            }

            return value;
        }

        public bool TryValidate(out string error)
        {
            return TryValidate(typeof(Vector3), false, out error);
        }

        public bool ValidateAndLog(UnityEngine.Object context = null)
        {
            return base.ValidateAndLog(typeof(Vector3), false, context);
        }
    }
}

