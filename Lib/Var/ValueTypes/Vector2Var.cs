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

            if (resolvedType == InputType.Service)
            {
                if (service == null)
                {
                    return Vector2.zero;
                }

                Vector2 serviceValue;
                if (service.TryGetOutputValue(out serviceValue))
                {
                    return serviceValue;
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
