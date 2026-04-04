#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GlobalContext))]
public class GlobalContextEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty entriesProperty = serializedObject.FindProperty("entries");
        if (entriesProperty != null)
        {
            DrawEntries(entriesProperty);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEntries(SerializedProperty entriesProperty)
    {
        EditorGUILayout.LabelField("Entries", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add GameObjectList", GUILayout.Width(160f)))
            {
                AddGameObjectListEntry();
                GUIUtility.ExitGUI();
            }
        }

        EditorGUILayout.Space(6f);

        for (int i = 0; i < entriesProperty.arraySize; i++)
        {
            SerializedProperty entryProperty = entriesProperty.GetArrayElementAtIndex(i);
            if (entryProperty == null)
            {
                continue;
            }

            SerializedProperty keyProperty = entryProperty.FindPropertyRelative("key");
            string title = keyProperty != null && !string.IsNullOrEmpty(keyProperty.stringValue)
                ? keyProperty.stringValue
                : "Entry " + i;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(entryProperty, true);
            DrawOutputProviderInfo(entryProperty);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remove", GUILayout.Width(90f)))
                {
                    entriesProperty.DeleteArrayElementAtIndex(i);
                    break;
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }
    }

    private static void DrawOutputProviderInfo(SerializedProperty entryProperty)
    {
        if (entryProperty == null)
        {
            return;
        }

        SerializedProperty valueTypeProperty = entryProperty.FindPropertyRelative("valueType");
        if (valueTypeProperty == null || valueTypeProperty.enumValueIndex != (int)GlobalValueType.OutputProvider)
        {
            return;
        }

        SerializedProperty providerProperty = entryProperty.FindPropertyRelative("outputProvider");
        MonoBehaviour provider = providerProperty != null ? providerProperty.objectReferenceValue as MonoBehaviour : null;
        if (provider == null)
        {
            EditorGUILayout.HelpBox("OutputProvider requires a MonoBehaviour with exactly one [Output] field.", MessageType.Warning);
            return;
        }

        FieldInfo outputField;
        string error;
        if (!GlobalTypeUtility.TryGetOutputProviderField(provider, out outputField, out error) || outputField == null)
        {
            return;
        }

        EditorGUILayout.LabelField("Resolved Output", outputField.Name + " : " + outputField.FieldType.Name);
    }

    private void AddGameObjectListEntry()
    {
        GlobalContext globalContext = target as GlobalContext;
        if (globalContext == null)
        {
            return;
        }

        Undo.RecordObject(globalContext, "Add Global GameObjectList Entry");
        globalContext.CreateGameObjectListEntry();
        EditorUtility.SetDirty(globalContext);
    }
}
#endif
