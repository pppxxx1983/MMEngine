using System;
using UnityEngine;

namespace SP
{
    public abstract class GlobalValueService : Service, IFlowPort
    {
        public bool HasEnterPort => false;
        public bool HasNextPort => false;

        [SerializeField]
        protected string key;

        public string Key
        {
            get => key;
            set => key = value;
        }

        public abstract Type TargetValueType { get; }

        protected bool TryGetGlobalValue<T>(out T result)
        {
            result = default;
            if (GlobalContext.ins == null || string.IsNullOrEmpty(key))
            {
                return false;
            }

            return GlobalContext.ins.TryGetValue(key, out result);
        }

        public bool TryGetSourceMetadata(out GlobalValueType valueType, out MonoBehaviour outputProvider)
        {
            valueType = GlobalValueType.GameObject;
            outputProvider = null;
            if (GlobalContext.ins == null || string.IsNullOrEmpty(key))
            {
                return false;
            }

            return GlobalContext.ins.TryGetEntryMetadata(key, out valueType, out outputProvider);
        }
    }
}
