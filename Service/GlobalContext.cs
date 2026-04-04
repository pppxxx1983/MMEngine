using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;



public enum GlobalValueType
{
    Int = 0,
    Float = 1,
    Bool = 2,
    String = 3,
    GameObject = 4,
    GameObjectList = 5,
    Vector2 = 6,
    Vector3 = 7,
    OutputProvider = 8
}

[Serializable]
public class GlobalEntry
{
    public string key;
    public GlobalValueType valueType = GlobalValueType.GameObject;
    public int intValue;
    public float floatValue;
    public Vector2 vector2Value;
    public Vector3 vector3Value;
    public bool boolValue;
    public string stringValue;
    public UnityEngine.Object objectValue;
    public List<GameObject> objectListValue = new List<GameObject>();
    public MonoBehaviour outputProvider;

    public bool KeyEquals(string input)
    {
        return !string.IsNullOrEmpty(key) && string.Equals(key, input, StringComparison.Ordinal);
    }

    public bool TryGetValue(Type targetType, out object value)
    {
        value = null;
        if (targetType == null)
        {
            return false;
        }

        NormalizeValueType();

        switch (valueType)
        {
            case GlobalValueType.Int:
                return GlobalTypeUtility.TryConvertPrimitive(intValue, targetType, out value);
            case GlobalValueType.Float:
                return GlobalTypeUtility.TryConvertPrimitive(floatValue, targetType, out value);
            case GlobalValueType.Vector2:
                if (targetType == typeof(Vector2))
                {
                    value = vector2Value;
                    return true;
                }
                return false;
            case GlobalValueType.Vector3:
                if (targetType == typeof(Vector3))
                {
                    value = vector3Value;
                    return true;
                }
                return false;
            case GlobalValueType.Bool:
                return GlobalTypeUtility.TryConvertPrimitive(boolValue, targetType, out value);
            case GlobalValueType.String:
                return GlobalTypeUtility.TryConvertPrimitive(stringValue, targetType, out value);
            case GlobalValueType.GameObject:
                return GlobalTypeUtility.TryConvertObject(objectValue, typeof(GameObject), targetType, out value);
            case GlobalValueType.OutputProvider:
                return GlobalTypeUtility.TryGetOutputProviderValue(outputProvider, targetType, out value);
            default:
                return false;
        }
    }

    public bool TryGetListValue(Type targetType, out object value)
    {
        value = null;
        if (targetType == null)
        {
            return false;
        }

        NormalizeValueType();

        if (valueType != GlobalValueType.GameObjectList)
        {
            return false;
        }

        return GlobalTypeUtility.TryConvertObjectList(objectListValue, typeof(GameObject), targetType, out value);
    }

    public bool MatchesTargetType(Type targetType)
    {
        if (targetType == null)
        {
            return false;
        }

        NormalizeValueType();

        if (targetType == typeof(GameObject))
        {
            return valueType == GlobalValueType.GameObject;
        }

        if (targetType == typeof(Transform))
        {
            return valueType == GlobalValueType.GameObject;
        }

        switch (valueType)
        {
            case GlobalValueType.Int:
                return targetType == typeof(int);
            case GlobalValueType.Float:
                return targetType == typeof(float);
            case GlobalValueType.Vector2:
                return targetType == typeof(Vector2);
            case GlobalValueType.Vector3:
                return targetType == typeof(Vector3);
            case GlobalValueType.Bool:
                return targetType == typeof(bool);
            case GlobalValueType.String:
                return targetType == typeof(string);
            case GlobalValueType.GameObject:
                return MatchesGameObjectTargetType(targetType);
            case GlobalValueType.OutputProvider:
                return MatchesOutputProviderTargetType(targetType);
            default:
                return false;
        }
    }

    public bool MatchesListTargetType(Type targetType)
    {
        if (targetType == null)
        {
            return false;
        }

        NormalizeValueType();

        if (valueType != GlobalValueType.GameObjectList)
        {
            return false;
        }

        return MatchesGameObjectListTargetType(targetType);
    }

    private bool MatchesGameObjectTargetType(Type targetType)
    {
        if (targetType == typeof(GameObject) || targetType == typeof(Transform))
        {
            return true;
        }

        if (!typeof(Component).IsAssignableFrom(targetType))
        {
            return false;
        }

        GameObject gameObject = objectValue as GameObject;
        return gameObject != null && gameObject.GetComponent(targetType) != null;
    }

    private bool MatchesGameObjectListTargetType(Type targetType)
    {
        if (targetType == typeof(GameObject) || targetType == typeof(Transform))
        {
            return true;
        }

        if (!typeof(Component).IsAssignableFrom(targetType))
        {
            return false;
        }

        if (objectListValue == null || objectListValue.Count == 0)
        {
            return true;
        }

        int i;
        for (i = 0; i < objectListValue.Count; i++)
        {
            GameObject gameObject = objectListValue[i];
            if (gameObject != null && gameObject.GetComponent(targetType) != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool MatchesOutputProviderTargetType(Type targetType)
    {
        if (outputProvider == null)
        {
            return false;
        }

        FieldInfo outputField;
        string error;
        if (!GlobalTypeUtility.TryGetOutputProviderField(outputProvider, out outputField, out error) || outputField == null)
        {
            return false;
        }

        return GlobalTypeUtility.IsOutputTypeCompatible(outputField.FieldType, targetType);
    }

    public void Normalize()
    {
        NormalizeValueType();

        if (valueType == GlobalValueType.GameObject && objectValue is Component gameObjectComponent)
        {
            objectValue = gameObjectComponent.gameObject;
        }

    }

    public bool TryValidate(out string error)
    {
        error = null;
        NormalizeValueType();
        if (valueType != GlobalValueType.OutputProvider)
        {
            return true;
        }

        FieldInfo outputField;
        return GlobalTypeUtility.TryGetOutputProviderField(outputProvider, out outputField, out error);
    }

    private void NormalizeValueType()
    {
        if (Enum.IsDefined(typeof(GlobalValueType), valueType))
        {
            return;
        }

        valueType = objectValue != null ? GlobalValueType.GameObject : GlobalValueType.String;
    }
}

public static class GlobalTypeUtility
{
    private const BindingFlags OutputFieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static bool TryConvertPrimitive(object sourceValue, Type targetType, out object value)
    {
        value = null;
        if (targetType == null)
        {
            return false;
        }

        if (sourceValue == null)
        {
            if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
            {
                return true;
            }

            return false;
        }

        Type actualTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (actualTargetType.IsInstanceOfType(sourceValue))
        {
            value = sourceValue;
            return true;
        }

        try
        {
            value = Convert.ChangeType(sourceValue, actualTargetType);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryConvertObject(UnityEngine.Object sourceObject, Type configuredType, Type targetType, out object value)
    {
        value = null;
        if (targetType == null)
        {
            return false;
        }

        if (sourceObject == null)
        {
            return !targetType.IsValueType;
        }

        UnityEngine.Object normalizedObject = NormalizeObjectReference(sourceObject, configuredType);
        if (normalizedObject == null)
        {
            return false;
        }

        if (targetType == typeof(GameObject))
        {
            GameObject gameObject = ExtractGameObject(normalizedObject);
            if (gameObject == null)
            {
                return false;
            }

            value = gameObject;
            return true;
        }

        if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
        {
            UnityEngine.Object typedObject = ResolveUnityObject(normalizedObject, targetType);
            if (typedObject == null)
            {
                return false;
            }

            value = typedObject;
            return true;
        }

        return false;
    }

    public static bool TryConvertObjectList(List<GameObject> sourceObjects, Type configuredType, Type targetType, out object value)
    {
        value = null;
        if (targetType == null)
        {
            return false;
        }

        if (sourceObjects == null)
        {
            return !targetType.IsValueType;
        }

        if (targetType == typeof(GameObject))
        {
            value = sourceObjects;
            return true;
        }

        List<UnityEngine.Object> results = new List<UnityEngine.Object>(sourceObjects.Count);
        int i;
        for (i = 0; i < sourceObjects.Count; i++)
        {
            object convertedValue;
            if (!TryConvertObject(sourceObjects[i], configuredType, targetType, out convertedValue))
            {
                continue;
            }

            UnityEngine.Object convertedObject = convertedValue as UnityEngine.Object;
            if (convertedObject != null)
            {
                results.Add(convertedObject);
            }
        }

        value = results;
        return true;
    }

    public static bool TryGetOutputProviderField(MonoBehaviour provider, out FieldInfo outputField, out string error)
    {
        outputField = null;
        error = null;
        if (provider == null)
        {
            error = "Output provider is null.";
            return false;
        }

        FieldInfo[] fields = provider.GetType().GetFields(OutputFieldFlags);
        int outputCount = 0;
        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo field = fields[i];
            if (field == null || !field.IsDefined(typeof(SP.OutputAttribute), true))
            {
                continue;
            }

            outputCount++;
            outputField = field;
        }

        if (outputCount == 1 && outputField != null)
        {
            return true;
        }

        if (outputCount == 0)
        {
            error = provider.GetType().Name + " has no [Output] field.";
            return false;
        }

        error = provider.GetType().Name + " has multiple [Output] fields. Only one is allowed.";
        outputField = null;
        return false;
    }

    public static bool TryGetOutputProviderValue(MonoBehaviour provider, Type targetType, out object value)
    {
        value = null;
        if (targetType == null)
        {
            return false;
        }

        FieldInfo outputField;
        string error;
        if (!TryGetOutputProviderField(provider, out outputField, out error) || outputField == null)
        {
            return false;
        }

        object rawValue = outputField.GetValue(provider);
        if (rawValue == null)
        {
            return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;
        }

        if (targetType.IsInstanceOfType(rawValue))
        {
            value = rawValue;
            return true;
        }

        UnityEngine.Object unityObject = rawValue as UnityEngine.Object;
        if (unityObject != null)
        {
            return TryConvertObject(unityObject, outputField.FieldType, targetType, out value);
        }

        return TryConvertPrimitive(rawValue, targetType, out value);
    }

    public static bool IsOutputTypeCompatible(Type outputType, Type targetType)
    {
        if (outputType == null || targetType == null)
        {
            return false;
        }

        if (targetType == typeof(GameObject))
        {
            return outputType == typeof(GameObject) || typeof(Component).IsAssignableFrom(outputType);
        }

        if (targetType == typeof(Transform))
        {
            return outputType == typeof(GameObject) || typeof(Component).IsAssignableFrom(outputType);
        }

        if (typeof(Component).IsAssignableFrom(targetType))
        {
            return outputType == typeof(GameObject) || targetType.IsAssignableFrom(outputType);
        }

        return targetType.IsAssignableFrom(outputType);
    }

    public static bool TryResolveSiblingOutputProvider(MonoBehaviour source, out MonoBehaviour provider)
    {
        provider = null;
        if (source == null || source.gameObject == null)
        {
            return false;
        }

        MonoBehaviour[] siblings = source.gameObject.GetComponents<MonoBehaviour>();
        if (siblings == null || siblings.Length == 0)
        {
            return false;
        }

        MonoBehaviour matched = null;
        int matchCount = 0;
        for (int i = 0; i < siblings.Length; i++)
        {
            MonoBehaviour candidate = siblings[i];
            if (candidate == null)
            {
                continue;
            }

            FieldInfo outputField;
            string error;
            if (!TryGetOutputProviderField(candidate, out outputField, out error) || outputField == null)
            {
                continue;
            }

            matchCount++;
            matched = candidate;
            if (matchCount > 1)
            {
                provider = null;
                return false;
            }
        }

        provider = matched;
        return provider != null;
    }

    private static UnityEngine.Object NormalizeObjectReference(UnityEngine.Object sourceObject, Type configuredType)
    {
        if (sourceObject == null || configuredType == null)
        {
            return null;
        }

        if (configuredType == typeof(GameObject))
        {
            return ExtractGameObject(sourceObject);
        }

        if (typeof(Component).IsAssignableFrom(configuredType))
        {
            Component component = ResolveUnityObject(sourceObject, configuredType) as Component;
            return component;
        }

        return ResolveUnityObject(sourceObject, configuredType);
    }

    private static UnityEngine.Object ResolveUnityObject(UnityEngine.Object sourceObject, Type targetType)
    {
        if (sourceObject == null || targetType == null)
        {
            return null;
        }

        Type actualTargetType = typeof(GameObject).IsAssignableFrom(targetType) ? typeof(GameObject) : targetType;
        if (actualTargetType.IsInstanceOfType(sourceObject))
        {
            return sourceObject;
        }

        GameObject gameObject = ExtractGameObject(sourceObject);
        if (gameObject == null)
        {
            return null;
        }

        if (actualTargetType == typeof(GameObject))
        {
            return gameObject;
        }

        if (typeof(Component).IsAssignableFrom(actualTargetType))
        {
            return gameObject.GetComponent(actualTargetType);
        }

        return null;
    }

    private static GameObject ExtractGameObject(UnityEngine.Object sourceObject)
    {
        GameObject gameObject = sourceObject as GameObject;
        if (gameObject != null)
        {
            return gameObject;
        }

        Component component = sourceObject as Component;
        if (component != null)
        {
            return component.gameObject;
        }

        return null;
    }
}
[DisallowMultipleComponent]
[DefaultExecutionOrder(-9999)]
public class GlobalContext:MonoBehaviour
{
    public static GlobalContext ins;
    [SerializeField]
    private List<GlobalEntry> entries = new List<GlobalEntry>();

    private readonly Dictionary<string, GlobalEntry> entryMap = new Dictionary<string, GlobalEntry>();
    private readonly Dictionary<Type, string[]> filteredValueKeysCache = new Dictionary<Type, string[]>();
    private readonly Dictionary<Type, string[]> filteredListKeysCache = new Dictionary<Type, string[]>();
    private string[] allKeysCache = new string[0];
    private bool isCacheDirty = true;

    private void Awake()
    {
        ins = this;
        Rebuild();
    }

    private void OnValidate()
    {
        Rebuild();
    }

    public bool TryGetValue<T>(string key, out T value)
    {
        value = default(T);

        object rawValue;
        if (!TryGetValue(key, typeof(T), out rawValue))
        {
            return false;
        }

        value = (T)rawValue;
        return true;
    }

    public bool TryGetValue(string key, Type targetType, out object value)
    {
        EnsureCache();
        value = null;
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        GlobalEntry entry;
        if (!entryMap.TryGetValue(key, out entry) || entry == null)
        {
            return false;
        }

        return entry.TryGetValue(targetType, out value);
    }

    public bool TryGetEntryMetadata(string key, out GlobalValueType valueType, out MonoBehaviour outputProvider)
    {
        EnsureCache();
        valueType = GlobalValueType.GameObject;
        outputProvider = null;
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        GlobalEntry entry;
        if (!entryMap.TryGetValue(key, out entry) || entry == null)
        {
            return false;
        }

        valueType = entry.valueType;
        outputProvider = entry.outputProvider;
        return true;
    }

    public bool TryGetListValue<T>(string key, out List<T> value) where T : UnityEngine.Object
    {
        value = null;

        object rawValue;
        if (!TryGetListValue(key, typeof(T), out rawValue))
        {
            return false;
        }

        if (typeof(T) == typeof(GameObject))
        {
            value = rawValue as List<T>;
            return value != null;
        }

        List<UnityEngine.Object> rawList = rawValue as List<UnityEngine.Object>;
        if (rawList == null)
        {
            return false;
        }

        value = new List<T>(rawList.Count);
        int i;
        for (i = 0; i < rawList.Count; i++)
        {
            T typedValue = rawList[i] as T;
            if (typedValue != null)
            {
                value.Add(typedValue);
            }
        }

        return true;
    }

    public bool TryGetListValue(string key, Type targetType, out object value)
    {
        EnsureCache();
        value = null;
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        GlobalEntry entry;
        if (!entryMap.TryGetValue(key, out entry) || entry == null)
        {
            return false;
        }

        return entry.TryGetListValue(targetType, out value);
    }

    public string[] GetKeys()
    {
        EnsureCache();
        return allKeysCache;
    }

    public string[] GetKeys(Type targetType)
    {
        return GetKeys(targetType, false);
    }

    public string[] GetKeys(Type targetType, bool expectsList)
    {
        EnsureCache();
        targetType = NormalizeLookupType(targetType, ref expectsList);
        if (entryMap.Count == 0 || targetType == null)
        {
            return new string[0];
        }

        Dictionary<Type, string[]> cache = expectsList ? filteredListKeysCache : filteredValueKeysCache;
        string[] cachedKeys;
        if (cache.TryGetValue(targetType, out cachedKeys))
        {
            return cachedKeys;
        }

        List<string> keys = new List<string>(entryMap.Count);
        foreach (KeyValuePair<string, GlobalEntry> pair in entryMap)
        {
            if (pair.Value == null)
            {
                continue;
            }

            bool matches = expectsList
                ? pair.Value.MatchesListTargetType(targetType)
                : pair.Value.MatchesTargetType(targetType);
            if (matches)
            {
                keys.Add(pair.Key);
            }
        }

        cachedKeys = keys.ToArray();
        cache[targetType] = cachedKeys;
        return cachedKeys;
    }

    private static Type NormalizeLookupType(Type targetType, ref bool expectsList)
    {
        if (targetType == null)
        {
            return null;
        }

        if (string.Equals(targetType.FullName, "SP.IntVar", StringComparison.Ordinal))
        {
            expectsList = false;
            return typeof(int);
        }

        if (string.Equals(targetType.FullName, "SP.FloatVar", StringComparison.Ordinal))
        {
            expectsList = false;
            return typeof(float);
        }

        if (string.Equals(targetType.FullName, "SP.StringVar", StringComparison.Ordinal))
        {
            expectsList = false;
            return typeof(string);
        }

        if (string.Equals(targetType.FullName, "SP.Vector2Var", StringComparison.Ordinal))
        {
            expectsList = false;
            return typeof(Vector2);
        }

        if (string.Equals(targetType.FullName, "SP.Vector3Var", StringComparison.Ordinal))
        {
            expectsList = false;
            return typeof(Vector3);
        }

        Type current = targetType;
        while (current != null)
        {
            if (current.IsGenericType)
            {
                Type genericTypeDefinition = current.GetGenericTypeDefinition();
                string genericTypeDefinitionName = genericTypeDefinition.FullName;
                if (string.Equals(genericTypeDefinitionName, "SP.MMVar`1", StringComparison.Ordinal))
                {
                    expectsList = false;
                    Type[] genericArguments = current.GetGenericArguments();
                    return genericArguments != null && genericArguments.Length == 1 ? genericArguments[0] : null;
                }

                if (string.Equals(genericTypeDefinitionName, "SP.MMListVar`1", StringComparison.Ordinal))
                {
                    expectsList = true;
                    Type[] genericArguments = current.GetGenericArguments();
                    return genericArguments != null && genericArguments.Length == 1 ? genericArguments[0] : null;
                }
            }

            current = current.BaseType;
        }

        return targetType;
    }

    public bool AddOrUpdateGameObject(string key, GameObject value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning("Global key is empty.", this);
            return false;
        }

        GlobalEntry entry = GetOrCreateEntry(key.Trim());
        if (entry == null)
        {
            return false;
        }

        entry.key = key.Trim();
        entry.valueType = GlobalValueType.GameObject;
        entry.objectValue = value;
        entry.objectListValue = entry.objectListValue ?? new List<GameObject>();
        MarkDirtyAndRebuild();
        return true;
    }

    public bool AddOrUpdateGameObjectList(string key, IEnumerable<GameObject> values)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning("Global key is empty.", this);
            return false;
        }

        GlobalEntry entry = GetOrCreateEntry(key.Trim());
        if (entry == null)
        {
            return false;
        }

        entry.key = key.Trim();
        entry.valueType = GlobalValueType.GameObjectList;
        entry.objectValue = null;
        entry.objectListValue = entry.objectListValue ?? new List<GameObject>();
        entry.objectListValue.Clear();

        if (values != null)
        {
            foreach (GameObject value in values)
            {
                if (value != null)
                {
                    entry.objectListValue.Add(value);
                }
            }
        }

        MarkDirtyAndRebuild();
        return true;
    }

    public bool AddOrUpdateGameObjectList(string key, params GameObject[] values)
    {
        return AddOrUpdateGameObjectList(key, (IEnumerable<GameObject>)values);
    }

    public string CreateGameObjectListEntry(string preferredKey = "GameObjectList")
    {
        string uniqueKey = GenerateUniqueKey(preferredKey);
        GlobalEntry entry = GetOrCreateEntry(uniqueKey);
        if (entry == null)
        {
            return string.Empty;
        }

        entry.key = uniqueKey;
        entry.valueType = GlobalValueType.GameObjectList;
        entry.intValue = 0;
        entry.floatValue = 0f;
        entry.vector2Value = Vector2.zero;
        entry.vector3Value = Vector3.zero;
        entry.boolValue = false;
        entry.stringValue = string.Empty;
        entry.objectValue = null;
        entry.objectListValue = new List<GameObject>();
        MarkDirtyAndRebuild();
        return uniqueKey;
    }

    public bool RemoveEntry(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || entries == null)
        {
            return false;
        }

        string normalizedKey = key.Trim();
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            GlobalEntry entry = entries[i];
            if (entry == null || !entry.KeyEquals(normalizedKey))
            {
                continue;
            }

            entries.RemoveAt(i);
            MarkDirtyAndRebuild();
            return true;
        }

        return false;
    }

    private GlobalEntry GetOrCreateEntry(string key)
    {
        if (entries == null)
        {
            entries = new List<GlobalEntry>();
        }

        int i;
        for (i = 0; i < entries.Count; i++)
        {
            GlobalEntry entry = entries[i];
            if (entry != null && entry.KeyEquals(key))
            {
                return entry;
            }
        }

        GlobalEntry newEntry = new GlobalEntry
        {
            key = key,
            objectListValue = new List<GameObject>()
        };
        entries.Add(newEntry);
        return newEntry;
    }

    private string GenerateUniqueKey(string preferredKey)
    {
        string baseKey = string.IsNullOrWhiteSpace(preferredKey) ? "GlobalKey" : preferredKey.Trim();
        string candidate = baseKey;
        int suffix = 1;

        while (ContainsKey(candidate))
        {
            candidate = baseKey + suffix;
            suffix++;
        }

        return candidate;
    }

    private bool ContainsKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || entries == null)
        {
            return false;
        }

        int i;
        for (i = 0; i < entries.Count; i++)
        {
            GlobalEntry entry = entries[i];
            if (entry != null && entry.KeyEquals(key))
            {
                return true;
            }
        }

        return false;
    }

    private void MarkDirtyAndRebuild()
    {
        isCacheDirty = true;
        Rebuild();
    }

    private void Rebuild()
    {
        entryMap.Clear();
        filteredValueKeysCache.Clear();
        filteredListKeysCache.Clear();
        if (entries == null)
        {
            allKeysCache = new string[0];
            isCacheDirty = false;
            return;
        }

        int i;
        for (i = 0; i < entries.Count; i++)
        {
            GlobalEntry entry = entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.key))
            {
                continue;
            }

            entry.Normalize();
            string entryError;
            if (!entry.TryValidate(out entryError) && !string.IsNullOrEmpty(entryError))
            {
                bool skipEmptyProviderWarning = entry.valueType == GlobalValueType.OutputProvider;
                if (!skipEmptyProviderWarning)
                {
                    Debug.LogWarning("Global entry \"" + entry.key + "\" is invalid. " + entryError, this);
                }
            }
            if (entryMap.ContainsKey(entry.key))
            {
                Debug.LogWarning("Duplicate global key detected: " + entry.key, this);
            }

            entryMap[entry.key] = entry;
        }

        allKeysCache = new string[entryMap.Count];
        entryMap.Keys.CopyTo(allKeysCache, 0);
        isCacheDirty = false;
    }

    private void EnsureCache()
    {
        if (!isCacheDirty)
        {
            return;
        }

        Rebuild();
    }

    private void OnEnable()
    {
        isCacheDirty = true;
    }

    private void OnDisable()
    {
        if (ins == this)
        {
            ins = null;
        }
    }
}
