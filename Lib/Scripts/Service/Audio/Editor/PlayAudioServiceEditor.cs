#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SP.Editor
{
    [CustomEditor(typeof(PlayAudioService))]
    public class PlayAudioServiceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptField();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
            DrawProperty("audioName");
            DrawProperty("playMode");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Common", EditorStyles.boldLabel);
            DrawProperty("volume");
            DrawProperty("spatialBlend");
            DrawProperty("useSelfPosition");

            SerializedProperty playModeProp = serializedObject.FindProperty("playMode");
            if (playModeProp != null)
            {
                AudioPlayMode mode = (AudioPlayMode)playModeProp.enumValueIndex;

                if (mode == AudioPlayMode.Repeat)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Repeat", EditorStyles.boldLabel);
                    DrawProperty("repeatCount");
                    DrawProperty("repeatInterval");
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                MonoScript script = MonoScript.FromMonoBehaviour((MonoBehaviour)target);
                EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
            }
        }

        private void DrawProperty(string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, true);
            }
        }
    }
}
#endif
