using System.Collections.Generic;
using UnityEngine;

public enum ResourceCategory
{
    Prefab = 0,
    Effect = 1,
    Audio = 2,
    Sprite = 3,
    Executor = 4, // 【新增】图执行器（逻辑节点）
    Service = 5   // 【新增】行为节点
}

[DisallowMultipleComponent]
[DefaultExecutionOrder(-9997)]
public class ResourceCenter : MonoBehaviour
{
    [System.Serializable]
    public sealed class PrefabEntry
    {
        public GameObject prefab;
        public bool usePooling;
        public int initialSize = 0;
    }

    [System.Serializable]
    public sealed class EffectEntry
    {
        public GameObject effectPrefab;
        public bool usePooling;
        public int initialSize = 0;
    }

    [System.Serializable]
    public sealed class AudioEntry
    {
        public AudioClip audioClip;
    }

    [System.Serializable]
    public sealed class SpriteEntry
    {
        public Sprite sprite;
    }

    [System.Serializable]
    public sealed class ExecutorEntry
    {
        public GameObject prefab;
        public bool usePooling = true;
        public int initialSize = 5;
    }

    [System.Serializable]
    public sealed class ServiceEntry
    {
        public GameObject prefab;
        public bool usePooling = true;
        public int initialSize = 5;
    }

    private sealed class PoolBucket
    {
        public string name;
        public ResourceCategory category;
        public GameObject sourcePrefab;
        public Transform root;
        public readonly List<GameObject> available = new List<GameObject>();
    }

    [Header("Prefab 资源")]
    public List<PrefabEntry> prefabs = new List<PrefabEntry>();

    [Header("Effect 资源")]
    public List<EffectEntry> effects = new List<EffectEntry>();

    [Header("Audio 资源")]
    public List<AudioEntry> audios = new List<AudioEntry>();

    [Header("Sprite 资源")]
    public List<SpriteEntry> sprites = new List<SpriteEntry>();

    [Header("逻辑节点资源 (Executor)")]
    public List<ExecutorEntry> executors = new List<ExecutorEntry>();

    [Header("行为节点资源 (独立Service)")]
    public List<ServiceEntry> services = new List<ServiceEntry>();

    private readonly Dictionary<string, GameObject> _prefabMap = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, GameObject> _effectMap = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, AudioClip> _audioMap = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, Sprite> _spriteMap = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, GameObject> _executorMap = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, GameObject> _serviceMap = new Dictionary<string, GameObject>();

    private readonly Dictionary<string, PoolBucket> _prefabBuckets = new Dictionary<string, PoolBucket>();
    private readonly Dictionary<string, PoolBucket> _effectBuckets = new Dictionary<string, PoolBucket>();
    private readonly Dictionary<string, PoolBucket> _executorBuckets = new Dictionary<string, PoolBucket>();
    private readonly Dictionary<string, PoolBucket> _serviceBuckets = new Dictionary<string, PoolBucket>();

    private Transform _pooledPrefabsRoot;
    private Transform _pooledEffectsRoot;
    private Transform _pooledExecutorsRoot;
    private Transform _pooledServicesRoot;


    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        ClearRuntimeState();

        if (!BuildPrefabRegistry()) return;
        if (!BuildEffectRegistry()) return;
        if (!BuildAudioRegistry()) return;
        if (!BuildSpriteRegistry()) return;
        if (!BuildExecutorRegistry()) return;
        if (!BuildServiceRegistry()) return;

        EnsurePoolRoots();
        BuildPrefabBuckets();
        BuildEffectBuckets();
        BuildExecutorBuckets();
        BuildServiceBuckets();

        IsInitialized = true;
    }

    public string[] GetPrefabNames()
    {
        return CollectPrefabNamesFromConfig();
    }

    public string[] GetEffectNames()
    {
        return CollectEffectNamesFromConfig();
    }

    public string[] GetAudioNames()
    {
        return CollectAudioNamesFromConfig();
    }

    public string[] GetSpriteNames()
    {
        return CollectSpriteNamesFromConfig();
    }

    public string[] GetExecutorNames()
    {
        return CollectNames(executors, entry => entry != null ? entry.prefab : null);
    }

    public string[] GetServiceNames()
    {
        return CollectNames(services, entry => entry != null ? entry.prefab : null);
    }

    public AudioClip GetAudioClip(string name)
    {
        if (!EnsureReady()) return null;
        if (string.IsNullOrEmpty(name)) return null;

        AudioClip clip;
        if (_audioMap.TryGetValue(name, out clip))
            return clip;

        Debug.LogError("ResourceCenter 找不到 Audio 资源：" + name, this);
        return null;
    }

    public Sprite GetSprite(string name)
    {
        if (!EnsureReady()) return null;
        if (string.IsNullOrEmpty(name)) return null;

        Sprite sprite;
        if (_spriteMap.TryGetValue(name, out sprite))
            return sprite;

        Debug.LogError("ResourceCenter 找不到 Sprite 资源：" + name, this);
        return null;
    }

    public GameObject SpawnPrefab(string name, Transform parent = null)
    {
        return SpawnPrefab(name, Vector3.zero, Quaternion.identity, parent, false);
    }

    public GameObject SpawnPrefab(string name, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        return SpawnPrefab(name, position, rotation, parent, true);
    }

    public GameObject SpawnEffect(string name, Transform parent = null)
    {
        return SpawnEffect(name, Vector3.zero, Quaternion.identity, parent, false);
    }

    public GameObject SpawnEffect(string name, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        return SpawnEffect(name, position, rotation, parent, true);
    }


    public void Release(GameObject instance)
    {
        if (instance == null)
            return;

        ResourceInstanceMarker marker = instance.GetComponent<ResourceInstanceMarker>();
        if (marker == null)
        {
            Debug.LogWarning("Release 失败：对象 [" + instance.name + "] 不是 ResourceCenter 创建的实例。", instance);
            return;
        }

        if (marker.owner != this)
        {
            Debug.LogWarning("Release 失败：对象 [" + instance.name + "] 不属于当前 ResourceCenter。", instance);
            return;
        }

        if (marker.inPool)
        {
            Debug.LogWarning("Release 失败：对象 [" + instance.name + "] 已经在池中。", instance);
            return;
        }

        if (!marker.isPooled)
        {
            Destroy(instance);
            return;
        }

        PoolBucket bucket = GetBucket(marker.category, marker.resourceName);
        if (bucket == null)
        {
            Debug.LogWarning("Release 失败：找不到对象 [" + instance.name + "] 对应的池 Bucket。", instance);
            Destroy(instance);
            return;
        }

        ReturnToBucket(instance, marker, bucket);
    }


    private bool BuildPrefabRegistry()
    {
        HashSet<string> names = new HashSet<string>();

        for (int i = 0; i < prefabs.Count; i++)
        {
            PrefabEntry entry = prefabs[i];
            if (entry == null || entry.prefab == null)
            {
                Debug.LogError("ResourceCenter Prefab 配置错误：第 " + i + " 项 prefab 为空。", this);
                return false;
            }

            string name = entry.prefab.name;
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("ResourceCenter Prefab 配置错误：第 " + i + " 项 prefab 名称为空。", this);
                return false;
            }

            if (!names.Add(name))
            {
                Debug.LogError("ResourceCenter Prefab 配置错误：存在重复名称 [" + name + "]。", this);
                return false;
            }

            _prefabMap.Add(name, entry.prefab);
        }

        return true;
    }

    private bool BuildEffectRegistry()
    {
        HashSet<string> names = new HashSet<string>();

        for (int i = 0; i < effects.Count; i++)
        {
            EffectEntry entry = effects[i];
            if (entry == null || entry.effectPrefab == null)
            {
                Debug.LogError("ResourceCenter Effect 配置错误：第 " + i + " 项 effectPrefab 为空。", this);
                return false;
            }

            string name = entry.effectPrefab.name;
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("ResourceCenter Effect 配置错误：第 " + i + " 项 effectPrefab 名称为空。", this);
                return false;
            }

            if (!names.Add(name))
            {
                Debug.LogError("ResourceCenter Effect 配置错误：存在重复名称 [" + name + "]。", this);
                return false;
            }

            _effectMap.Add(name, entry.effectPrefab);
        }

        return true;
    }

    private bool BuildAudioRegistry()
    {
        HashSet<string> names = new HashSet<string>();

        for (int i = 0; i < audios.Count; i++)
        {
            AudioEntry entry = audios[i];
            if (entry == null || entry.audioClip == null)
            {
                Debug.LogError("ResourceCenter Audio 配置错误：第 " + i + " 项 audioClip 为空。", this);
                return false;
            }

            string name = entry.audioClip.name;
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("ResourceCenter Audio 配置错误：第 " + i + " 项 audioClip 名称为空。", this);
                return false;
            }

            if (!names.Add(name))
            {
                Debug.LogError("ResourceCenter Audio 配置错误：存在重复名称 [" + name + "]。", this);
                return false;
            }

            _audioMap.Add(name, entry.audioClip);
        }

        return true;
    }

    private bool BuildSpriteRegistry()
    {
        HashSet<string> names = new HashSet<string>();

        for (int i = 0; i < sprites.Count; i++)
        {
            SpriteEntry entry = sprites[i];
            if (entry == null || entry.sprite == null)
            {
                Debug.LogError("ResourceCenter Sprite 配置错误：第 " + i + " 项 sprite 为空。", this);
                return false;
            }

            string name = entry.sprite.name;
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("ResourceCenter Sprite 配置错误：第 " + i + " 项 sprite 名称为空。", this);
                return false;
            }

            if (!names.Add(name))
            {
                Debug.LogError("ResourceCenter Sprite 配置错误：存在重复名称 [" + name + "]。", this);
                return false;
            }

            _spriteMap.Add(name, entry.sprite);
        }

        return true;
    }

    private bool BuildExecutorRegistry()
    {
        HashSet<string> names = new HashSet<string>();

        for (int i = 0; i < executors.Count; i++)
        {
            ExecutorEntry entry = executors[i];
            if (entry == null || entry.prefab == null)
            {
                Debug.LogError("ResourceCenter Executor 配置错误：第 " + i + " 项 prefab 为空。", this);
                return false;
            }

            string name = entry.prefab.name;
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("ResourceCenter Executor 配置错误：第 " + i + " 项 prefab 名称为空。", this);
                return false;
            }

            if (!names.Add(name))
            {
                Debug.LogError("ResourceCenter Executor 配置错误：存在重复名称 [" + name + "]。", this);
                return false;
            }

            _executorMap.Add(name, entry.prefab);
        }

        return true;
    }

    private bool BuildServiceRegistry()
    {
        HashSet<string> names = new HashSet<string>();

        for (int i = 0; i < services.Count; i++)
        {
            ServiceEntry entry = services[i];
            if (entry == null || entry.prefab == null)
            {
                Debug.LogError("ResourceCenter Service 配置错误：第 " + i + " 项 prefab 为空。", this);
                return false;
            }

            string name = entry.prefab.name;
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("ResourceCenter Service 配置错误：第 " + i + " 项 prefab 名称为空。", this);
                return false;
            }

            if (!names.Add(name))
            {
                Debug.LogError("ResourceCenter Service 配置错误：存在重复名称 [" + name + "]。", this);
                return false;
            }

            _serviceMap.Add(name, entry.prefab);
        }

        return true;
    }

    private void BuildPrefabBuckets()
    {
        for (int i = 0; i < prefabs.Count; i++)
        {
            PrefabEntry entry = prefabs[i];
            if (entry == null || entry.prefab == null || !entry.usePooling)
                continue;

            string name = entry.prefab.name;
            PoolBucket bucket = CreateBucket(name, ResourceCategory.Prefab, entry.prefab, _pooledPrefabsRoot);
            _prefabBuckets.Add(name, bucket);

            int initialSize = Mathf.Max(0, entry.initialSize);
            for (int count = 0; count < initialSize; count++)
            {
                GameObject instance = CreatePooledInstance(bucket);
                bucket.available.Add(instance);
            }
        }
    }

    private void BuildEffectBuckets()
    {
        for (int i = 0; i < effects.Count; i++)
        {
            EffectEntry entry = effects[i];
            if (entry == null || entry.effectPrefab == null || !entry.usePooling)
                continue;

            string name = entry.effectPrefab.name;
            PoolBucket bucket = CreateBucket(name, ResourceCategory.Effect, entry.effectPrefab, _pooledEffectsRoot);
            _effectBuckets.Add(name, bucket);

            int initialSize = Mathf.Max(0, entry.initialSize);
            for (int count = 0; count < initialSize; count++)
            {
                GameObject instance = CreatePooledInstance(bucket);
                bucket.available.Add(instance);
            }
        }
    }

    private void BuildExecutorBuckets()
    {
        for (int i = 0; i < executors.Count; i++)
        {
            ExecutorEntry entry = executors[i];
            if (entry == null || entry.prefab == null || !entry.usePooling)
                continue;

            string name = entry.prefab.name;
            PoolBucket bucket = CreateBucket(name, ResourceCategory.Executor, entry.prefab, _pooledExecutorsRoot);
            _executorBuckets.Add(name, bucket);

            int initialSize = Mathf.Max(0, entry.initialSize);
            for (int count = 0; count < initialSize; count++)
            {
                GameObject instance = CreatePooledInstance(bucket);
                bucket.available.Add(instance);
            }
        }
    }

    private void BuildServiceBuckets()
    {
        for (int i = 0; i < services.Count; i++)
        {
            ServiceEntry entry = services[i];
            if (entry == null || entry.prefab == null || !entry.usePooling)
                continue;

            string name = entry.prefab.name;
            PoolBucket bucket = CreateBucket(name, ResourceCategory.Service, entry.prefab, _pooledServicesRoot);
            _serviceBuckets.Add(name, bucket);

            int initialSize = Mathf.Max(0, entry.initialSize);
            for (int count = 0; count < initialSize; count++)
            {
                GameObject instance = CreatePooledInstance(bucket);
                bucket.available.Add(instance);
            }
        }
    }

    private void EnsurePoolRoots()
    {
        _pooledPrefabsRoot = CreateOrGetChildRoot("[PooledPrefabs]");
        _pooledEffectsRoot = CreateOrGetChildRoot("[PooledEffects]");
        _pooledExecutorsRoot = CreateOrGetChildRoot("[PooledExecutors]");
        _pooledServicesRoot = CreateOrGetChildRoot("[PooledServices]");
    }

    private Transform CreateOrGetChildRoot(string childName)
    {
        Transform child = transform.Find(childName);
        if (child != null)
            return child;

        GameObject go = new GameObject(childName);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go.transform;
    }

    private PoolBucket CreateBucket(string name, ResourceCategory category, GameObject sourcePrefab, Transform parentRoot)
    {
        GameObject bucketRootObject = new GameObject(name);
        bucketRootObject.transform.SetParent(parentRoot);
        bucketRootObject.transform.localPosition = Vector3.zero;
        bucketRootObject.transform.localRotation = Quaternion.identity;
        bucketRootObject.transform.localScale = Vector3.one;

        PoolBucket bucket = new PoolBucket();
        bucket.name = name;
        bucket.category = category;
        bucket.sourcePrefab = sourcePrefab;
        bucket.root = bucketRootObject.transform;
        return bucket;
    }

    private GameObject CreatePooledInstance(PoolBucket bucket)
    {
        GameObject instance = Instantiate(bucket.sourcePrefab, bucket.root);
        instance.name = bucket.sourcePrefab.name;
        SetupMarker(instance, bucket.name, bucket.category, true, true);
        instance.SetActive(false);
        return instance;
    }

    private GameObject SpawnPrefab(string name, Vector3 position, Quaternion rotation, Transform parent, bool applyWorldPose)
    {
        if (!EnsureReady()) return null;

        GameObject prefab;
        if (!_prefabMap.TryGetValue(name, out prefab))
        {
            Debug.LogError("ResourceCenter 找不到 Prefab 资源：" + name, this);
            return null;
        }

        PoolBucket bucket;
        if (_prefabBuckets.TryGetValue(name, out bucket))
        {
            GameObject instance = AcquireFromBucket(bucket);
            ActivateSpawnedInstance(instance, position, rotation, parent, applyWorldPose);
            return instance;
        }

        GameObject spawned = Instantiate(prefab, parent);
        spawned.name = prefab.name;
        SetupMarker(spawned, name, ResourceCategory.Prefab, false, false);
        ActivateSpawnedInstance(spawned, position, rotation, parent, applyWorldPose);
        return spawned;
    }

    private GameObject SpawnEffect(string name, Vector3 position, Quaternion rotation, Transform parent, bool applyWorldPose)
    {
        if (!EnsureReady()) return null;

        GameObject prefab;
        if (!_effectMap.TryGetValue(name, out prefab))
        {
            Debug.LogError("ResourceCenter 找不到 Effect 资源：" + name, this);
            return null;
        }

        PoolBucket bucket;
        if (_effectBuckets.TryGetValue(name, out bucket))
        {
            GameObject instance = AcquireFromBucket(bucket);
            ActivateSpawnedInstance(instance, position, rotation, parent, applyWorldPose);
            return instance;
        }

        GameObject spawned = Instantiate(prefab, parent);
        spawned.name = prefab.name;
        SetupMarker(spawned, name, ResourceCategory.Effect, false, false);
        ActivateSpawnedInstance(spawned, position, rotation, parent, applyWorldPose);
        return spawned;
    }

    private GameObject AcquireFromBucket(PoolBucket bucket)
    {
        GameObject instance = null;

        while (bucket.available.Count > 0)
        {
            int lastIndex = bucket.available.Count - 1;
            instance = bucket.available[lastIndex];
            bucket.available.RemoveAt(lastIndex);

            if (instance != null)
            {
                break;
            }

#if UNITY_EDITOR
            Debug.LogWarning($"[ResourceCenter] 对象池 [{bucket.name}] 中发现已销毁的实例引用，已自动跳过。", this);
#endif

            instance = null;
        }

        if (instance == null)
        {
            instance = CreatePooledInstance(bucket);
        }

        ResourceInstanceMarker marker = instance.GetComponent<ResourceInstanceMarker>();
        if (marker == null)
        {
            SetupMarker(instance, bucket.name, bucket.category, true, false);
        }
        else
        {
            marker.owner = this;
            marker.resourceName = bucket.name;
            marker.category = bucket.category;
            marker.isPooled = true;
            marker.inPool = false;
        }

        return instance;
    }

    private void ReturnToBucket(GameObject instance, ResourceInstanceMarker marker, PoolBucket bucket)
    {
        instance.SetActive(false);
        instance.transform.SetParent(bucket.root);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = GetSourcePrefabLocalScale(marker, bucket);

        marker.inPool = true;
        bucket.available.Add(instance);
    }

    private void ActivateSpawnedInstance(GameObject instance, Vector3 position, Quaternion rotation, Transform parent, bool applyWorldPose)
    {
        if (instance == null)
            return;

        instance.transform.SetParent(parent);

        if (applyWorldPose)
        {
            instance.transform.SetPositionAndRotation(position, rotation);
        }

        ResourceInstanceMarker marker = instance.GetComponent<ResourceInstanceMarker>();
        if (marker != null)
        {
            instance.transform.localScale = GetSourcePrefabLocalScale(marker, null);
        }

        instance.SetActive(true);
    }

    private void SetupMarker(GameObject instance, string resourceName, ResourceCategory category, bool isPooled, bool inPool)
    {
        ResourceInstanceMarker marker = instance.GetComponent<ResourceInstanceMarker>();
        if (marker == null)
            marker = instance.AddComponent<ResourceInstanceMarker>();

        marker.owner = this;
        marker.resourceName = resourceName;
        marker.category = category;
        marker.isPooled = isPooled;
        marker.inPool = inPool;
    }

    private PoolBucket GetBucket(ResourceCategory category, string name)
    {
        switch (category)
        {
            case ResourceCategory.Prefab:
            {
                PoolBucket prefabBucket;
                _prefabBuckets.TryGetValue(name, out prefabBucket);
                return prefabBucket;
            }
            case ResourceCategory.Effect:
            {
                PoolBucket effectBucket;
                _effectBuckets.TryGetValue(name, out effectBucket);
                return effectBucket;
            }
            case ResourceCategory.Executor:
            {
                PoolBucket executorBucket;
                _executorBuckets.TryGetValue(name, out executorBucket);
                return executorBucket;
            }
            case ResourceCategory.Service:
            {
                PoolBucket serviceBucket;
                _serviceBuckets.TryGetValue(name, out serviceBucket);
                return serviceBucket;
            }
            default:
                return null;
        }
    }

    
    private Vector3 GetSourcePrefabLocalScale(ResourceInstanceMarker marker, PoolBucket fallbackBucket)
    {
        if (fallbackBucket != null && fallbackBucket.sourcePrefab != null)
            return fallbackBucket.sourcePrefab.transform.localScale;

        if (marker == null)
            return Vector3.one;

        GameObject sourcePrefab = GetSourcePrefab(marker.category, marker.resourceName);
        if (sourcePrefab != null)
            return sourcePrefab.transform.localScale;

        return Vector3.one;
    }

    private GameObject GetSourcePrefab(ResourceCategory category, string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        switch (category)
        {
            case ResourceCategory.Prefab:
            {
                GameObject prefab;
                _prefabMap.TryGetValue(name, out prefab);
                return prefab;
            }
            case ResourceCategory.Effect:
            {
                GameObject effectPrefab;
                _effectMap.TryGetValue(name, out effectPrefab);
                return effectPrefab;
            }
            case ResourceCategory.Executor:
            {
                GameObject executorPrefab;
                _executorMap.TryGetValue(name, out executorPrefab);
                return executorPrefab;
            }
            case ResourceCategory.Service:
            {
                GameObject servicePrefab;
                _serviceMap.TryGetValue(name, out servicePrefab);
                return servicePrefab;
            }
            default:
                return null;
        }
    }
    
    private bool EnsureReady()
    {
        if (IsInitialized)
            return true;

        Debug.LogError("ResourceCenter 尚未成功初始化。", this);
        return false;
    }

    private string[] CollectPrefabNamesFromConfig()
    {
        return CollectNames(
            prefabs,
            entry => entry != null ? entry.prefab : null
        );
    }

    private string[] CollectEffectNamesFromConfig()
    {
        return CollectNames(
            effects,
            entry => entry != null ? entry.effectPrefab : null
        );
    }

    private string[] CollectAudioNamesFromConfig()
    {
        return CollectNames(
            audios,
            entry => entry != null ? entry.audioClip : null
        );
    }

    private string[] CollectSpriteNamesFromConfig()
    {
        return CollectNames(
            sprites,
            entry => entry != null ? entry.sprite : null
        );
    }

    private static string[] CollectNames<TEntry, TObject>(List<TEntry> entries, System.Func<TEntry, TObject> selector)
        where TObject : Object
    {
        if (entries == null || entries.Count == 0)
            return new string[0];

        List<string> result = new List<string>(entries.Count);
        HashSet<string> added = new HashSet<string>();

        for (int i = 0; i < entries.Count; i++)
        {
            TObject obj = selector(entries[i]);
            if (obj == null)
                continue;

            string name = obj.name;
            if (string.IsNullOrEmpty(name))
                continue;

            if (added.Add(name))
            {
                result.Add(name);
            }
        }

        return result.ToArray();
    }

    private void ClearRuntimeState()
    {
        IsInitialized = false;

        _prefabMap.Clear();
        _effectMap.Clear();
        _audioMap.Clear();
        _spriteMap.Clear();
        _executorMap.Clear();
        _serviceMap.Clear();

        _prefabBuckets.Clear();
        _effectBuckets.Clear();
        _executorBuckets.Clear();
        _serviceBuckets.Clear();

        if (_pooledPrefabsRoot != null)
            DestroyImmediateSafe(_pooledPrefabsRoot.gameObject);
        if (_pooledEffectsRoot != null)
            DestroyImmediateSafe(_pooledEffectsRoot.gameObject);
        if (_pooledExecutorsRoot != null)
            DestroyImmediateSafe(_pooledExecutorsRoot.gameObject);
        if (_pooledServicesRoot != null)
            DestroyImmediateSafe(_pooledServicesRoot.gameObject);

        _pooledPrefabsRoot = null;
        _pooledEffectsRoot = null;
        _pooledExecutorsRoot = null;
        _pooledServicesRoot = null;
    }

    private void DestroyImmediateSafe(GameObject go)
    {
        if (go == null)
            return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(go);
        else
            Destroy(go);
#else
        Destroy(go);
#endif
    }
}

public sealed class ResourceInstanceMarker : MonoBehaviour
{
    [HideInInspector] public ResourceCenter owner;
    [HideInInspector] public string resourceName;
    [HideInInspector] public ResourceCategory category;
    [HideInInspector] public bool isPooled;
    [HideInInspector] public bool inPool;
}