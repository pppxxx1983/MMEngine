using UnityEngine;

namespace SP
{
    [System.Serializable]
    public class IntVar : MMVar
    {
        [ShowIf(nameof(type), (int)InputType.Default)]
        public int value;

        public IntVar()
        {
            tag = "Input Int";
        }

        public int Get()
        {
            base.ValidateAndLog(typeof(int), false, null);

            if (type == InputType.Service)
            {
                if (service == null)
                {
                    return value;
                }

                int serviceValue;
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

                int globalValue;
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
            return TryValidate(typeof(int), false, out error);
        }

        public bool ValidateAndLog(UnityEngine.Object context = null)
        {
            return base.ValidateAndLog(typeof(int), false, context);
        }
    }
}
