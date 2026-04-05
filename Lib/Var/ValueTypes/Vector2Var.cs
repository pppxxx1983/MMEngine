using UnityEngine;

namespace SP
{
    [System.Serializable]
    public class Vector2Var : MMVar
    {
        public override bool SupportsDefaultInput => false;
        

        public Vector2Var()
        {
            tag = "Input Vector2";
        }

        public Vector2 Get()
        {
            base.ValidateAndLog(typeof(Vector2), false, null);
            InputType resolvedType = GetResolvedInputType();

            if (resolvedType == InputType.Output)
            {
                if (service == null)
                {
                    return Vector2.zero;
                }

                object rawValue;
                string error;
                if (OutputUtility.TryGetOutputValue(service, typeof(Vector2), out rawValue, out error) && rawValue is Vector2 resolvedValue)
                {
                    return resolvedValue;
                }

                return Vector2.zero;
            }

            if (resolvedType == InputType.Global)
            {
                if (GlobalContext.ins == null || string.IsNullOrEmpty(global))
                {
                    return Vector2.zero;
                }

                Vector2 globalValue;
                if (GlobalContext.ins.TryGetValue(global, out globalValue))
                {
                    return globalValue;
                }

                return Vector2.zero;
            }

            return Vector2.zero;
        }

        public bool TryValidate(out string error)
        {
            return TryValidate(typeof(Vector2), false, out error);
        }

        public bool ValidateAndLog(UnityEngine.Object context = null)
        {
            return base.ValidateAndLog(typeof(Vector2), false, context);
        }
    }
}

