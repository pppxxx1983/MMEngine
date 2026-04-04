using UnityEditor;
using UnityEngine;

namespace SP.Editor
{
    internal static class EditorFoldoutStateUtility
    {
        public static bool DrawFoldout(Object target, string keySuffix, string label, bool defaultValue = true)
        {
            string key = GetKey(target, keySuffix);
            bool current = SessionState.GetBool(key, defaultValue);
            bool next = EditorGUILayout.Foldout(current, label, true);
            if (next != current)
                SessionState.SetBool(key, next);

            return next;
        }

        private static string GetKey(Object target, string keySuffix)
        {
            int instanceId = target != null ? target.GetInstanceID() : 0;
            string typeName = target != null ? target.GetType().FullName : "null";
            return $"SP.EditorFoldout.{typeName}.{instanceId}.{keySuffix}";
        }
    }
}
