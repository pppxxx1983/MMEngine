using UnityEngine;

namespace SP
{
    [System.Serializable]
    public class FloatVar : MMVar
    {
        [ShowIf(nameof(type), (int)InputType.Default)]
        public float value;

        public FloatVar()
        {
            tag = "Input Float";
        }

        public float Get()
        {
            base.ValidateAndLog(typeof(float), false, null);

            if (type == InputType.Output)
            {
                if (service == null)
                {
                    return value;
                }

                object rawValue;
                string error;
                if (OutputUtility.TryGetOutputValue(service, typeof(float), out rawValue, out error) && rawValue is float resolvedValue)
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

                float globalValue;
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
            return TryValidate(typeof(float), false, out error);
        }

        public bool ValidateAndLog(UnityEngine.Object context = null)
        {
            return base.ValidateAndLog(typeof(float), false, context);
        }
    }
}

