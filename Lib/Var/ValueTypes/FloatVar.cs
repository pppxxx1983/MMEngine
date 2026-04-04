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

            if (type == InputType.Service)
            {
                if (service == null)
                {
                    return value;
                }

                float serviceValue;
                if (service.TryGetOutputValue(out serviceValue))
                {
                    return serviceValue;
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
