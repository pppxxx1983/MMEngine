#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SP;

namespace SP.Editor
{
    [CustomPropertyDrawer(typeof(GlobalEntry))]
    public class GlobalEntryDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty keyProperty = property.FindPropertyRelative(nameof(GlobalEntry.key));
            SerializedProperty valueTypeProperty = property.FindPropertyRelative(nameof(GlobalEntry.valueType));
            SerializedProperty intValueProperty = property.FindPropertyRelative(nameof(GlobalEntry.intValue));
            SerializedProperty floatValueProperty = property.FindPropertyRelative(nameof(GlobalEntry.floatValue));
            SerializedProperty vector2ValueProperty = property.FindPropertyRelative(nameof(GlobalEntry.vector2Value));
            SerializedProperty vector3ValueProperty = property.FindPropertyRelative(nameof(GlobalEntry.vector3Value));
            SerializedProperty boolValueProperty = property.FindPropertyRelative(nameof(GlobalEntry.boolValue));
            SerializedProperty stringValueProperty = property.FindPropertyRelative(nameof(GlobalEntry.stringValue));
            SerializedProperty objectValueProperty = property.FindPropertyRelative(nameof(GlobalEntry.objectValue));
            SerializedProperty objectListValueProperty = property.FindPropertyRelative(nameof(GlobalEntry.objectListValue));
            SerializedProperty outputProviderProperty = property.FindPropertyRelative(nameof(GlobalEntry.outputProvider));

            float y = position.y;
            Rect keyRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(keyRect, keyProperty);
            y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            Rect typeRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(typeRect, valueTypeProperty);
            y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            GlobalValueType valueType = (GlobalValueType)valueTypeProperty.enumValueIndex;
            if (valueType == GlobalValueType.OutputProvider && outputProviderProperty != null)
            {
                Rect valueRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
                MonoBehaviour current = outputProviderProperty.objectReferenceValue as MonoBehaviour;
                MonoBehaviour next = EditorGUI.ObjectField(valueRect, "Value", current, typeof(MonoBehaviour), true) as MonoBehaviour;
                if (!ReferenceEquals(current, next))
                {
                    if (next == null)
                    {
                        outputProviderProperty.objectReferenceValue = null;
                    }
                    else
                    {
                        System.Reflection.FieldInfo outputField;
                        string error;
                        if (GlobalTypeUtility.TryGetOutputProviderField(next, out outputField, out error))
                        {
                            outputProviderProperty.objectReferenceValue = next;
                        }
                    }
                }
            }
            else
            {
                SerializedProperty valueProperty = GetValueProperty(
                    valueType,
                    intValueProperty,
                    floatValueProperty,
                    vector2ValueProperty,
                    vector3ValueProperty,
                    boolValueProperty,
                    stringValueProperty,
                    objectValueProperty,
                    objectListValueProperty,
                    outputProviderProperty);
                Rect valueRect = new Rect(position.x, y, position.width, EditorGUI.GetPropertyHeight(valueProperty, true));
                EditorGUI.PropertyField(valueRect, valueProperty, new GUIContent("Value"), true);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueTypeProperty = property.FindPropertyRelative(nameof(GlobalEntry.valueType));
            SerializedProperty intValueProperty = property.FindPropertyRelative(nameof(GlobalEntry.intValue));
            SerializedProperty floatValueProperty = property.FindPropertyRelative(nameof(GlobalEntry.floatValue));
            SerializedProperty vector2ValueProperty = property.FindPropertyRelative(nameof(GlobalEntry.vector2Value));
            SerializedProperty vector3ValueProperty = property.FindPropertyRelative(nameof(GlobalEntry.vector3Value));
            SerializedProperty boolValueProperty = property.FindPropertyRelative(nameof(GlobalEntry.boolValue));
            SerializedProperty stringValueProperty = property.FindPropertyRelative(nameof(GlobalEntry.stringValue));
            SerializedProperty objectValueProperty = property.FindPropertyRelative(nameof(GlobalEntry.objectValue));
            SerializedProperty objectListValueProperty = property.FindPropertyRelative(nameof(GlobalEntry.objectListValue));
            SerializedProperty outputProviderProperty = property.FindPropertyRelative(nameof(GlobalEntry.outputProvider));

            GlobalValueType valueType = (GlobalValueType)valueTypeProperty.enumValueIndex;
            SerializedProperty valueProperty = GetValueProperty(
                valueType,
                intValueProperty,
                floatValueProperty,
                vector2ValueProperty,
                vector3ValueProperty,
                boolValueProperty,
                stringValueProperty,
                objectValueProperty,
                objectListValueProperty,
                outputProviderProperty);

            float height = EditorGUIUtility.singleLineHeight * 2f;
            height += EditorGUIUtility.standardVerticalSpacing * 2f;

            height += EditorGUI.GetPropertyHeight(valueProperty, true);
            return height;
        }

        private static SerializedProperty GetValueProperty(
            GlobalValueType valueType,
            SerializedProperty intValueProperty,
            SerializedProperty floatValueProperty,
            SerializedProperty vector2ValueProperty,
            SerializedProperty vector3ValueProperty,
            SerializedProperty boolValueProperty,
            SerializedProperty stringValueProperty,
            SerializedProperty objectValueProperty,
            SerializedProperty objectListValueProperty,
            SerializedProperty outputProviderProperty)
        {
            switch (valueType)
            {
                case GlobalValueType.Int:
                    return intValueProperty;
                case GlobalValueType.Float:
                    return floatValueProperty;
                case GlobalValueType.Vector2:
                    return vector2ValueProperty;
                case GlobalValueType.Vector3:
                    return vector3ValueProperty;
                case GlobalValueType.Bool:
                    return boolValueProperty;
                case GlobalValueType.String:
                    return stringValueProperty;
                case GlobalValueType.GameObjectList:
                    return objectListValueProperty;
                case GlobalValueType.OutputProvider:
                    return outputProviderProperty != null ? outputProviderProperty : objectValueProperty;
                default:
                    return objectValueProperty;
            }
        }
    }
}
#endif
