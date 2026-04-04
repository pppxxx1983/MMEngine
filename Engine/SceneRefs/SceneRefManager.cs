using System;
using System.Collections.Generic;
using UnityEngine;

namespace SP.SceneRefs
{
    /// <summary>
    /// 场景引用全局管理器。
    /// 特性：
    /// 1. 全局唯一
    /// 2. key = id, value = SceneRefObject 本身
    /// 3. 同一个对象只注册一次
    /// 4. 出现重复 id 时，自动刷新为新的唯一 id
    /// </summary>
    public sealed class SceneRefManager
    {
        private static SceneRefManager instance;

        private readonly Dictionary<string, SceneRefObject> idToObject = new Dictionary<string, SceneRefObject>();
        private readonly Dictionary<int, string> objectInstanceIdToId = new Dictionary<int, string>();

        private SceneRefManager()
        {
        }

        public static SceneRefManager Instance
        {
            get { return instance ?? (instance = new SceneRefManager()); }
        }

        public static bool HasInstance
        {
            get { return instance != null; }
        }

        public IReadOnlyDictionary<string, SceneRefObject> References
        {
            get { return idToObject; }
        }

        public void Register(SceneRefObject target)
        {
            if (target == null)
            {
                return;
            }

            int instanceId = target.GetInstanceID();

            RemoveOldMapping(instanceId);
            EnsureUniqueId(target);

            if (string.IsNullOrEmpty(target.Id))
            {
                return;
            }

            idToObject[target.Id] = target;
            objectInstanceIdToId[instanceId] = target.Id;
        }

        public void Unregister(SceneRefObject target)
        {
            if (target == null)
            {
                return;
            }

            int instanceId = target.GetInstanceID();
            string currentId;
            if (!objectInstanceIdToId.TryGetValue(instanceId, out currentId))
            {
                return;
            }

            objectInstanceIdToId.Remove(instanceId);

            SceneRefObject mappedObject;
            if (idToObject.TryGetValue(currentId, out mappedObject) && mappedObject == target)
            {
                idToObject.Remove(currentId);
            }
        }

        public bool Contains(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            return idToObject.ContainsKey(id);
        }

        public bool TryGet(string id, out SceneRefObject target)
        {
            if (string.IsNullOrEmpty(id))
            {
                target = null;
                return false;
            }

            if (idToObject.TryGetValue(id, out target))
            {
                if (target != null)
                {
                    return true;
                }

                idToObject.Remove(id);
            }

            target = null;
            return false;
        }

        public bool TryGet<T>(string id, out T target) where T : Component
        {
            SceneRefObject sceneRefObject;
            if (TryGet(id, out sceneRefObject))
            {
                target = sceneRefObject.GetComponent<T>();
                return target != null;
            }

            target = null;
            return false;
        }

        public bool TryGetGameObject(string id, out GameObject target)
        {
            SceneRefObject sceneRefObject;
            if (TryGet(id, out sceneRefObject))
            {
                target = sceneRefObject.gameObject;
                return target != null;
            }

            target = null;
            return false;
        }

        private void EnsureUniqueId(SceneRefObject target)
        {
            if (string.IsNullOrEmpty(target.Id))
            {
                target.SetId(CreateId());
            }

            SceneRefObject existedObject;
            while (idToObject.TryGetValue(target.Id, out existedObject) && existedObject != null && existedObject != target)
            {
                target.SetId(CreateId());
            }
        }

        private void RemoveOldMapping(int instanceId)
        {
            string oldId;
            if (!objectInstanceIdToId.TryGetValue(instanceId, out oldId))
            {
                return;
            }

            objectInstanceIdToId.Remove(instanceId);

            SceneRefObject oldTarget;
            if (idToObject.TryGetValue(oldId, out oldTarget) && oldTarget != null && oldTarget.GetInstanceID() == instanceId)
            {
                idToObject.Remove(oldId);
            }
        }

        private string CreateId()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
