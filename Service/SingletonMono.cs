using UnityEngine;

namespace SP
{
    public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _ins;
        public static T Ins
        {
            get
            {
                if (_ins != null)
                {
                    return _ins;
                }

                _ins = FindObjectOfType<T>();
                if (_ins != null)
                {
                    return _ins;
                }

                GameObject go = new GameObject(typeof(T).Name);
                _ins = go.AddComponent<T>();
                return _ins;
            }
        }

        protected virtual void Awake()
        {
            T current = this as T;
            if (_ins != null && _ins != current)
            {
                Destroy(gameObject);
                return;
            }

            _ins = current;
        }

        protected bool IsSingletonInstance => _ins == this as T;
    }
}
