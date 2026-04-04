using UnityEngine;

namespace SP
{
    [System.Serializable]
    public class StringVar : MMVar
    {
        [ShowIf(nameof(type), (int)InputType.Default)]
        public string value = string.Empty;

        public StringVar()
        {
            tag = "Input String";
        }

        public string Get()
        {
            base.ValidateAndLog(typeof(string), false, null);

            if (type == InputType.Service)
            {
                if (service == null)
                {
                    return value;
                }

                string serviceValue;
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

                string globalValue;
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
            return TryValidate(typeof(string), false, out error);
        }

        public bool ValidateAndLog(UnityEngine.Object context = null)
        {
            return base.ValidateAndLog(typeof(string), false, context);
        }
    }
}
