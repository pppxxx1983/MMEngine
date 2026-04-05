using System;
using UnityEngine;

namespace SP
{
    public abstract class GlobalValueService<T> : GlobalValueService
    {
        protected abstract T DefaultValue { get; }

        protected abstract void SetOutputValue(T value);

        protected virtual bool TryResolveValue(out T value)
        {
            return TryGetGlobalValue(out value);
        }

        protected virtual void RefreshOutputValue()
        {
            if (TryResolveValue(out T resolved))
            {
                SetOutputValue(resolved);
                return;
            }

            SetOutputValue(DefaultValue);
        }
        private void OnEnable()
        {
            RefreshOutputValue();
        }

        private void Update()
        {
            RefreshOutputValue();
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            RefreshOutputValue();
        }
#endif
    }
}




