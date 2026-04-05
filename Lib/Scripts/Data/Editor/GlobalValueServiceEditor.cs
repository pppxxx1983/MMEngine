#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SP.Editor
{
    [CustomEditor(typeof(GlobalValueService), true)]
    public class GlobalValueServiceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GlobalValueService service = target as GlobalValueService;
            SerializedProperty keyProperty = serializedObject.FindProperty("key");

            DrawScriptField(target as MonoBehaviour);
            if (service != null && keyProperty != null)
            {
                DrawKeyPopup(service, keyProperty);
                DrawSourceInfo(service, keyProperty.stringValue);
            }
            else if (keyProperty != null)
            {
                EditorGUILayout.PropertyField(keyProperty);
            }

            DrawPropertiesExcluding(serializedObject, "m_Script", "key");
            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawScriptField(MonoBehaviour behaviour)
        {
            MonoScript script = behaviour != null ? MonoScript.FromMonoBehaviour(behaviour) : null;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
            }
        }

        private static void DrawKeyPopup(GlobalValueService service, SerializedProperty keyProperty)
        {
            Type targetType = service.TargetValueType;
            if (targetType == null)
            {
                EditorGUILayout.PropertyField(keyProperty);
                return;
            }

            GlobalContext global = FindGlobalInstance();
            if (global == null)
            {
                EditorGUILayout.PropertyField(keyProperty, new GUIContent("Key"));
                EditorGUILayout.HelpBox("GlobalContext instance not found. Showing text input fallback.", MessageType.Info);
                return;
            }

            string[] keys = global.GetKeys(targetType, false);
            if (keys == null || keys.Length == 0)
            {
                EditorGUILayout.Popup("Key", 0, new[] { "<No Matching Keys>" });
                return;
            }

            string currentKey = keyProperty.stringValue;
            int selectedIndex = Array.IndexOf(keys, currentKey);
            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }

            string[] labels = BuildKeyLabels(global, keys);
            int nextIndex = EditorGUILayout.Popup("Key", selectedIndex, labels);
            if (nextIndex >= 0 && nextIndex < keys.Length)
            {
                keyProperty.stringValue = keys[nextIndex];
            }
        }

        private static string[] BuildKeyLabels(GlobalContext global, string[] keys)
        {
            List<string> labels = new List<string>(keys.Length);
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                if (!global.TryGetEntryMetadata(key, out GlobalValueType valueType, out MonoBehaviour provider))
                {
                    labels.Add(key + " [Unknown]");
                    continue;
                }

                if (valueType == GlobalValueType.OutputProvider && provider != null)
                {
                    FieldInfo outputField;
                    string error;
                    string outputLabel = GlobalTypeUtility.TryGetOutputProviderField(provider, out outputField, out error) && outputField != null
                        ? outputField.FieldType.Name
                        : "Unknown";
                    labels.Add(key + " [OutputProvider:" + provider.GetType().Name + " -> " + outputLabel + "]");
                    continue;
                }

                labels.Add(key + " [Value:" + valueType + "]");
            }

            return labels.ToArray();
        }

        private static void DrawSourceInfo(GlobalValueService service, string key)
        {
            if (service == null || string.IsNullOrEmpty(key))
            {
                return;
            }

            GlobalContext global = FindGlobalInstance();
            if (global == null)
            {
                EditorGUILayout.HelpBox("GlobalContext instance not found.", MessageType.Warning);
                return;
            }

            if (!global.TryGetEntryMetadata(key, out GlobalValueType valueType, out MonoBehaviour provider))
            {
                EditorGUILayout.HelpBox("Selected key is not found in GlobalContext.", MessageType.Warning);
                return;
            }

            if (valueType == GlobalValueType.OutputProvider)
            {
                if (provider == null)
                {
                    EditorGUILayout.HelpBox("Source: OutputProvider (Missing reference)", MessageType.Warning);
                    return;
                }

                FieldInfo outputField;
                string error;
                if (GlobalTypeUtility.TryGetOutputProviderField(provider, out outputField, out error) && outputField != null)
                {
                    EditorGUILayout.HelpBox(
                        "Source: OutputProvider\nProvider: " + provider.GetType().Name + "\nOutput: " + outputField.Name + " : " + outputField.FieldType.Name,
                        MessageType.Info);
                    return;
                }

                EditorGUILayout.HelpBox("Source: OutputProvider\nProvider: " + provider.GetType().Name, MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox("Source: Value (" + valueType + ")", MessageType.None);
        }

        private static GlobalContext FindGlobalInstance()
        {
            if (GlobalContext.ins != null)
            {
                return GlobalContext.ins;
            }

            GlobalContext[] globals = Resources.FindObjectsOfTypeAll<GlobalContext>();
            if (globals == null || globals.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < globals.Length; i++)
            {
                GlobalContext global = globals[i];
                if (global == null || EditorUtility.IsPersistent(global))
                {
                    continue;
                }

                return global;
            }

            return globals[0];
        }
    }
}
#endif

