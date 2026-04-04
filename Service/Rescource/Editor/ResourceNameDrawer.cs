#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ResourceNameAttribute))]
public sealed class ResourceNameDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property == null || property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        ResourceNameAttribute resourceNameAttribute = attribute as ResourceNameAttribute;
        if (resourceNameAttribute == null)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        string[] names = GetResourceNames(resourceNameAttribute.category);
        if (names == null || names.Length == 0)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.EndProperty();
            return;
        }

        int selectedIndex = 0;
        string currentValue = property.stringValue;
        if (!string.IsNullOrEmpty(currentValue))
        {
            int index = System.Array.IndexOf(names, currentValue);
            if (index >= 0)
            {
                selectedIndex = index;
            }
        }

        EditorGUI.BeginProperty(position, label, property);
        int nextIndex = EditorGUI.Popup(position, label.text, selectedIndex, names);
        if (nextIndex >= 0 && nextIndex < names.Length)
        {
            property.stringValue = names[nextIndex];
        }
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    private static string[] GetResourceNames(ResourceCategory category)
    {
        ResourceCenter resourceCenter = FindResourceCenter();
        if (resourceCenter == null)
        {
            return new string[0];
        }

        switch (category)
        {
            case ResourceCategory.Prefab:
                return resourceCenter.GetPrefabNames();
            case ResourceCategory.Effect:
                return resourceCenter.GetEffectNames();
            case ResourceCategory.Audio:
                return resourceCenter.GetAudioNames();
            case ResourceCategory.Sprite:
                return resourceCenter.GetSpriteNames();
            case ResourceCategory.Executor:
                return resourceCenter.GetExecutorNames();
            case ResourceCategory.Service:
                return resourceCenter.GetServiceNames();
            default:
                return new string[0];
        }
    }

    private static ResourceCenter FindResourceCenter()
    {
        if (Root.Instance != null && Root.Instance.resourceCenter != null)
        {
            return Root.Instance.resourceCenter;
        }

        Root[] roots = Resources.FindObjectsOfTypeAll<Root>();
        if (roots != null)
        {
            for (int i = 0; i < roots.Length; i++)
            {
                Root root = roots[i];
                if (root == null || EditorUtility.IsPersistent(root))
                {
                    continue;
                }

                if (root.resourceCenter != null)
                {
                    return root.resourceCenter;
                }

                ResourceCenter childCenter = root.GetComponentInChildren<ResourceCenter>(true);
                if (childCenter != null)
                {
                    return childCenter;
                }
            }
        }

        ResourceCenter[] centers = Resources.FindObjectsOfTypeAll<ResourceCenter>();
        if (centers == null || centers.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < centers.Length; i++)
        {
            ResourceCenter center = centers[i];
            if (center == null || EditorUtility.IsPersistent(center))
            {
                continue;
            }

            return center;
        }

        return centers[0];
    }
}
#endif
