using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SP.SceneRefs
{
    /// <summary>
    /// 挂在场景对象上的唯一引用组件。
    /// 职责：
    /// 1. 持有唯一 ID
    /// 2. 在编辑器下自动生成 ID
    /// 3. 在启用、校验时自动注册到 SceneRefManager
    /// 4. 在禁用、销毁时自动从 SceneRefManager 移除
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class SceneRefObject : MonoBehaviour
    {
        [SerializeField]
        private string id;

        public string Id
        {
            get { return id; }
        }

        public void SetId(string newId)
        {
            if (id == newId)
            {
                return;
            }

            id = newId;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
            }
#endif
        }

        private void Reset()
        {
            RegisterSelf();
        }

        private void OnEnable()
        {
            RegisterSelf();
        }

        private void OnValidate()
        {
            RegisterSelf();
        }

        private void OnDisable()
        {
            if (!SceneRefManager.HasInstance)
            {
                return;
            }

            SceneRefManager.Instance.Unregister(this);
        }

        private void OnDestroy()
        {
            if (!SceneRefManager.HasInstance)
            {
                return;
            }

            SceneRefManager.Instance.Unregister(this);
        }

        private void RegisterSelf()
        {
            if (!gameObject.scene.IsValid())
            {
                return;
            }

            SceneRefManager.Instance.Register(this);
        }
    }
}
