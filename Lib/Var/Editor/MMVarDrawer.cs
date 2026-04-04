#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SP.Editor
{
    [CustomPropertyDrawer(typeof(MMVar), true)]
    public class MMVarDrawer : PropertyDrawer
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty tagProperty = property.FindPropertyRelative(FieldNames.Tag);
            SerializedProperty typeProperty = property.FindPropertyRelative(FieldNames.Type);
            NormalizeTypeProperty(typeProperty, fieldInfo != null ? fieldInfo.FieldType : null);
            InputType currentInputType = GetInputType(typeProperty, fieldInfo != null ? fieldInfo.FieldType : null);
            System.Type expectedGlobalValueType = FindSupportedValueType(fieldInfo != null ? fieldInfo.FieldType : null);
            List<SerializedProperty> visibleValueProperties = GetVisibleValueProperties(property, typeProperty, expectedGlobalValueType);

            DrawHeaderRow(position, property, label, tagProperty, typeProperty);

            float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = previousIndent + 1;

            int i;
            for (i = 0; i < visibleValueProperties.Count; i++)
            {
                SerializedProperty valueProperty = visibleValueProperties[i];
                float valueHeight = GetValuePropertyHeightInstance(valueProperty, currentInputType, expectedGlobalValueType);
                Rect valueRect = new Rect(position.x, y, position.width, valueHeight);
                DrawValueProperty(valueRect, valueProperty, currentInputType, expectedGlobalValueType);
                y += valueHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            EditorGUI.indentLevel = previousIndent;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            SerializedProperty typeProperty = property.FindPropertyRelative(FieldNames.Type);
            NormalizeTypeProperty(typeProperty, fieldInfo != null ? fieldInfo.FieldType : null);
            InputType currentInputType = GetInputType(typeProperty, fieldInfo != null ? fieldInfo.FieldType : null);
            System.Type expectedGlobalValueType = FindSupportedValueType(fieldInfo != null ? fieldInfo.FieldType : null);
            List<SerializedProperty> visibleValueProperties = GetVisibleValueProperties(property, typeProperty, expectedGlobalValueType);
            if (visibleValueProperties.Count > 0)
            {
                height += EditorGUIUtility.standardVerticalSpacing;
                height += GetCombinedPropertyHeightInstance(visibleValueProperties, currentInputType, expectedGlobalValueType);
            }

            return height;
        }

        private void DrawHeaderRow(Rect position, SerializedProperty property, GUIContent label, SerializedProperty tagProperty, SerializedProperty typeProperty)
        {
            Rect rowRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            float spacing = 2f;
            Vector2 inputLabelSize = GUI.skin.label.CalcSize(new GUIContent("[Input]"));
            float inputLabelWidth = inputLabelSize.x + 2f;
            float sourceTypeWidth = 90f;
            string typeName = GetSupportedDisplayTypeName(fieldInfo != null ? fieldInfo.FieldType : null);
            if (string.IsNullOrEmpty(typeName))
            {
                typeName = tagProperty != null && !string.IsNullOrEmpty(tagProperty.stringValue) ? tagProperty.stringValue : label.text;
            }

            float typeLabelWidth = Mathf.Max(120f, rowRect.width - inputLabelWidth - sourceTypeWidth - spacing * 2f);
            Rect inputLabelRect = new Rect(rowRect.x, rowRect.y, inputLabelWidth, rowRect.height);
            Rect typeLabelRect = new Rect(inputLabelRect.xMax + spacing, rowRect.y, typeLabelWidth, rowRect.height);
            Rect sourceTypeRect = new Rect(typeLabelRect.xMax + spacing, rowRect.y, rowRect.xMax - (typeLabelRect.xMax + spacing), rowRect.height);

            EditorGUI.LabelField(inputLabelRect, "[Input]");
            EditorGUI.LabelField(typeLabelRect, typeName);
            if (typeProperty != null)
            {
                DrawInputTypePopup(sourceTypeRect, typeProperty, fieldInfo != null ? fieldInfo.FieldType : null);
            }
        }

        private static void DrawInputTypePopup(Rect position, SerializedProperty typeProperty, System.Type varFieldType)
        {
            if (typeProperty == null)
            {
                return;
            }

            NormalizeTypeProperty(typeProperty, varFieldType);
            InputType[] allowedTypes = GetAllowedInputTypes(varFieldType);
            if (allowedTypes == null || allowedTypes.Length == 0)
            {
                EditorGUI.PropertyField(position, typeProperty, GUIContent.none);
                return;
            }

            string[] options = new string[allowedTypes.Length];
            int selectedIndex = 0;
            InputType currentType = (InputType)typeProperty.enumValueIndex;
            for (int i = 0; i < allowedTypes.Length; i++)
            {
                options[i] = allowedTypes[i].ToString();
                if (allowedTypes[i] == currentType)
                {
                    selectedIndex = i;
                }
            }

            int nextIndex = EditorGUI.Popup(position, selectedIndex, options);
            if (nextIndex >= 0 && nextIndex < allowedTypes.Length)
            {
                typeProperty.enumValueIndex = (int)allowedTypes[nextIndex];
            }
        }

        private static void NormalizeTypeProperty(SerializedProperty typeProperty, System.Type varFieldType)
        {
            if (typeProperty == null)
            {
                return;
            }

            InputType currentType = (InputType)typeProperty.enumValueIndex;
            if (IsInputTypeSupported(varFieldType, currentType))
            {
                return;
            }

            typeProperty.enumValueIndex = (int)GetFallbackInputType(varFieldType);
        }

        private List<SerializedProperty> GetVisibleValueProperties(SerializedProperty property, SerializedProperty typeProperty, System.Type expectedValueType)
        {
            List<SerializedProperty> valueProperties = GetValueProperties(property);
            if (typeProperty == null || valueProperties.Count == 0)
            {
                return valueProperties;
            }

            bool hasShowIfField = false;
            List<SerializedProperty> visibleProperties = new List<SerializedProperty>();

            int i;
            for (i = 0; i < valueProperties.Count; i++)
            {
                SerializedProperty valueProperty = valueProperties[i];
                if (ShouldHideDefaultObjectProperty(valueProperty, typeProperty, expectedValueType))
                {
                    continue;
                }

                ShowIfAttribute[] attributes = GetShowIfAttributes(valueProperty);
                if (attributes.Length == 0)
                {
                    continue;
                }

                hasShowIfField = true;
                if (MatchesCondition(attributes, typeProperty))
                {
                    visibleProperties.Add(valueProperty);
                }
            }

            return hasShowIfField ? visibleProperties : valueProperties;
        }

        private static bool ShouldHideDefaultObjectProperty(SerializedProperty property, SerializedProperty typeProperty, System.Type expectedValueType)
        {
            if (property == null || typeProperty == null)
            {
                return false;
            }

            InputType inputType = GetInputType(typeProperty, null);
            if (inputType != InputType.Default)
            {
                return false;
            }

            if (property.name != FieldNames.Object && property.name != FieldNames.Objects)
            {
                return false;
            }

            if (expectedValueType == null)
            {
                return false;
            }

            if (expectedValueType == typeof(GameObject) || typeof(Component).IsAssignableFrom(expectedValueType))
            {
                return false;
            }

            return true;
        }

        private static class FieldNames
        {
            public const string Tag = "tag";
            public const string Type = "type";
            public const string Global = "global";
            public const string Service = "service";
            public const string Object = "obj";
            public const string Objects = "objs";
        }

        private static List<SerializedProperty> GetValueProperties(SerializedProperty property)
        {
            List<SerializedProperty> properties = new List<SerializedProperty>();
            if (property == null)
            {
                return properties;
            }

            SerializedProperty iterator = property.Copy();
            SerializedProperty endProperty = iterator.GetEndProperty();
            int targetDepth = property.depth + 1;
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                enterChildren = false;

                if (iterator.depth != targetDepth)
                {
                    continue;
                }

                if (iterator.name == FieldNames.Tag || iterator.name == FieldNames.Type)
                {
                    continue;
                }

                properties.Add(iterator.Copy());
            }

            return properties;
        }

        private static bool MatchesCondition(ShowIfAttribute[] attributes, SerializedProperty conditionProperty)
        {
            if (attributes == null || attributes.Length == 0 || conditionProperty == null)
            {
                return false;
            }

            int currentValue;
            switch (conditionProperty.propertyType)
            {
                case SerializedPropertyType.Enum:
                    currentValue = conditionProperty.enumValueIndex;
                    break;
                case SerializedPropertyType.Integer:
                    currentValue = conditionProperty.intValue;
                    break;
                default:
                    return false;
            }

            int i;
            for (i = 0; i < attributes.Length; i++)
            {
                ShowIfAttribute attribute = attributes[i];
                if (attribute == null)
                {
                    continue;
                }

                if (attribute.ConditionFieldName == conditionProperty.name && attribute.ExpectedValue == currentValue)
                {
                    return true;
                }
            }

            return false;
        }

        private ShowIfAttribute[] GetShowIfAttributes(SerializedProperty property)
        {
            FieldInfo childFieldInfo = GetChildFieldInfo(property);
            if (childFieldInfo == null)
            {
                return new ShowIfAttribute[0];
            }

            object[] rawAttributes = childFieldInfo.GetCustomAttributes(typeof(ShowIfAttribute), true);
            if (rawAttributes == null || rawAttributes.Length == 0)
            {
                return new ShowIfAttribute[0];
            }

            List<ShowIfAttribute> attributes = new List<ShowIfAttribute>(rawAttributes.Length);
            int i;
            for (i = 0; i < rawAttributes.Length; i++)
            {
                ShowIfAttribute attribute = rawAttributes[i] as ShowIfAttribute;
                if (attribute != null)
                {
                    attributes.Add(attribute);
                }
            }

            return attributes.ToArray();
        }

        private FieldInfo GetChildFieldInfo(SerializedProperty property)
        {
            if (property == null || fieldInfo == null)
            {
                return null;
            }

            return GetFieldFromTypeHierarchy(fieldInfo.FieldType, property.name);
        }

        private static FieldInfo GetFieldFromTypeHierarchy(System.Type type, string fieldName)
        {
            while (type != null)
            {
                FieldInfo match = type.GetField(fieldName, FieldFlags);
                if (match != null)
                {
                    return match;
                }

                type = type.BaseType;
            }

            return null;
        }

        private static GUIContent GetFoldoutLabel(GUIContent label, SerializedProperty tagProperty, System.Type fieldType)
        {
            string typeName = GetSupportedDisplayTypeName(fieldType);
            if (!string.IsNullOrEmpty(typeName))
            {
                return new GUIContent(typeName);
            }

            if (tagProperty == null || string.IsNullOrEmpty(tagProperty.stringValue))
            {
                return label;
            }

            return new GUIContent(tagProperty.stringValue);
        }

        private static float GetCombinedPropertyHeight(List<SerializedProperty> properties, InputType currentInputType, System.Type expectedValueType)
        {
            if (properties == null || properties.Count == 0)
            {
                return 0f;
            }

            float height = 0f;
            int i;
            for (i = 0; i < properties.Count; i++)
            {
                height += GetValuePropertyHeight(properties[i], currentInputType, expectedValueType);
                height += EditorGUIUtility.standardVerticalSpacing;
            }

            return height - EditorGUIUtility.standardVerticalSpacing;
        }

        private float GetCombinedPropertyHeightInstance(List<SerializedProperty> properties, InputType currentInputType, System.Type expectedValueType)
        {
            if (properties == null || properties.Count == 0)
            {
                return 0f;
            }

            float height = 0f;
            int i;
            for (i = 0; i < properties.Count; i++)
            {
                height += GetValuePropertyHeightInstance(properties[i], currentInputType, expectedValueType);
                height += EditorGUIUtility.standardVerticalSpacing;
            }

            return height - EditorGUIUtility.standardVerticalSpacing;
        }

        private static InputType GetInputType(SerializedProperty typeProperty, System.Type varFieldType)
        {
            if (typeProperty == null)
            {
                return InputType.Default;
            }

            InputType rawType = (InputType)typeProperty.enumValueIndex;
            if (IsInputTypeSupported(varFieldType, rawType))
            {
                return rawType;
            }

            return GetFallbackInputType(varFieldType);
        }

        private static InputType[] GetAllowedInputTypes(System.Type varFieldType)
        {
            List<InputType> allowed = new List<InputType>();
            InputType[] allTypes = (InputType[])System.Enum.GetValues(typeof(InputType));
            for (int i = 0; i < allTypes.Length; i++)
            {
                if (IsInputTypeSupported(varFieldType, allTypes[i]))
                {
                    allowed.Add(allTypes[i]);
                }
            }

            if (allowed.Count == 0)
            {
                allowed.Add(InputType.Default);
            }

            return allowed.ToArray();
        }

        private static bool IsInputTypeSupported(System.Type varFieldType, InputType inputType)
        {
            if (varFieldType == null)
            {
                return true;
            }

            object instance = null;
            try
            {
                instance = System.Activator.CreateInstance(varFieldType);
            }
            catch
            {
                return true;
            }

            MMVar singleVar = instance as MMVar;
            if (singleVar != null)
            {
                return singleVar.IsInputTypeSupported(inputType);
            }

            MMListVar listVar = instance as MMListVar;
            if (listVar != null)
            {
                return listVar.IsInputTypeSupported(inputType);
            }

            return true;
        }

        private static InputType GetFallbackInputType(System.Type varFieldType)
        {
            if (varFieldType == null)
            {
                return InputType.Default;
            }

            object instance = null;
            try
            {
                instance = System.Activator.CreateInstance(varFieldType);
            }
            catch
            {
                return InputType.Default;
            }

            MMVar singleVar = instance as MMVar;
            if (singleVar != null)
            {
                return singleVar.GetFallbackInputType();
            }

            MMListVar listVar = instance as MMListVar;
            if (listVar != null)
            {
                return listVar.GetFallbackInputType();
            }

            return InputType.Default;
        }

        private static float GetValuePropertyHeight(SerializedProperty property, InputType currentInputType, System.Type expectedValueType)
        {
            if (IsGlobalKeyProperty(property, currentInputType))
            {
                return EditorGUIUtility.singleLineHeight;
            }

            if (IsServiceProperty(property, currentInputType))
            {
                float height = EditorGUIUtility.singleLineHeight;
                string validationError = GetServiceValidationError(property, expectedValueType);
                if (!string.IsNullOrEmpty(validationError))
                {
                    height += EditorGUIUtility.standardVerticalSpacing;
                    height += GetHelpBoxHeight(validationError);
                }

                return height;
            }

            return EditorGUI.GetPropertyHeight(property, true);
        }

        private float GetValuePropertyHeightInstance(SerializedProperty property, InputType currentInputType, System.Type expectedValueType)
        {
            if (IsServiceProperty(property, currentInputType))
            {
                float height = EditorGUIUtility.singleLineHeight;
                string validationError = GetServiceValidationError(property.objectReferenceValue as Service, expectedValueType, IsListFieldType());
                if (!string.IsNullOrEmpty(validationError))
                {
                    height += EditorGUIUtility.standardVerticalSpacing;
                    height += GetHelpBoxHeight(validationError);
                }

                return height;
            }

            return GetValuePropertyHeight(property, currentInputType, expectedValueType);
        }

        private void DrawValueProperty(Rect position, SerializedProperty property, InputType currentInputType, System.Type expectedValueType)
        {
            if (IsGlobalKeyProperty(property, currentInputType))
            {
                DrawGlobalKeyPopup(position, property, expectedValueType);
                return;
            }

            if (IsDefaultObjectProperty(property, currentInputType))
            {
                DrawDefaultObjectField(position, property, expectedValueType);
                return;
            }

            if (IsServiceProperty(property, currentInputType))
            {
                DrawServiceField(position, property, expectedValueType);
                return;
            }

            EditorGUI.PropertyField(position, property, true);
        }

        private static bool IsGlobalKeyProperty(SerializedProperty property, InputType currentInputType)
        {
            return currentInputType == InputType.Global && property != null && property.name == FieldNames.Global;
        }

        private static bool IsDefaultObjectProperty(SerializedProperty property, InputType currentInputType)
        {
            if (currentInputType != InputType.Default || property == null)
            {
                return false;
            }

            return property.name == FieldNames.Object || property.name == FieldNames.Objects;
        }

        private static bool IsServiceProperty(SerializedProperty property, InputType currentInputType)
        {
            return currentInputType == InputType.Service && property != null && property.name == FieldNames.Service;
        }

        private void DrawGlobalKeyPopup(Rect position, SerializedProperty property, System.Type expectedValueType)
        {
            string[] keys = GetGlobalKeys(expectedValueType, IsListFieldType());
            string currentKey = property != null ? property.stringValue : null;
            string sourceDisplayText;
            Object sourceDisplayObject;
            BuildGlobalSourceDisplay(currentKey, out sourceDisplayText, out sourceDisplayObject);

            float keyPopupWidth = CalculateKeyPopupWidth(currentKey);
            float leftWidth = Mathf.Max(120f, position.width - keyPopupWidth - 4f);
            Rect sourceRect = new Rect(position.x, position.y, leftWidth, position.height);
            Rect popupRect = new Rect(sourceRect.xMax + 4f, position.y, position.width - leftWidth - 4f, position.height);

            if (GUI.Button(sourceRect, sourceDisplayText, EditorStyles.objectField))
            {
                if (sourceDisplayObject != null)
                {
                    GameObject sourceGameObject = sourceDisplayObject as GameObject;
                    Component sourceComponent = sourceDisplayObject as Component;
                    if (sourceGameObject != null)
                    {
                        EditorGUIUtility.PingObject(sourceGameObject);
                    }
                    else if (sourceComponent != null && sourceComponent.gameObject != null)
                    {
                        EditorGUIUtility.PingObject(sourceComponent.gameObject);
                    }
                    else
                    {
                        EditorGUIUtility.PingObject(sourceDisplayObject);
                    }
                }
            }

            if (keys.Length == 0)
            {
                EditorGUI.Popup(popupRect, 0, new[] { "<No Global Keys>" });
                return;
            }

            int selectedIndex = 0;
            string currentValue = property.stringValue;
            if (!string.IsNullOrEmpty(currentValue))
            {
                int existingIndex = System.Array.IndexOf(keys, currentValue);
                if (existingIndex >= 0)
                {
                    selectedIndex = existingIndex;
                }
            }

            int nextIndex = EditorGUI.Popup(popupRect, selectedIndex, keys);
            if (nextIndex >= 0 && nextIndex < keys.Length)
            {
                property.stringValue = keys[nextIndex];
            }
        }

        private static float CalculateKeyPopupWidth(string currentKey)
        {
            string text = string.IsNullOrEmpty(currentKey) ? "<Key>" : currentKey;
            float contentWidth = EditorStyles.popup.CalcSize(new GUIContent(text)).x + 20f;
            return Mathf.Clamp(contentWidth, 90f, 260f);
        }

        private void BuildGlobalSourceDisplay(string globalKey, out string displayText, out Object displayObject)
        {
            string sourceInfo;
            if (!TryGetGlobalSourceInfo(globalKey, out sourceInfo, out displayObject) || string.IsNullOrEmpty(sourceInfo))
            {
                displayText = "None";
                return;
            }

            displayText = sourceInfo;
        }

        private static bool TryGetGlobalSourceInfo(string key, out string info, out Object displayObject)
        {
            info = null;
            displayObject = null;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            GlobalContext global = FindGlobalInstance();
            if (global == null)
            {
                return false;
            }

            SerializedObject globalObject = new SerializedObject(global);
            SerializedProperty entriesProperty = globalObject.FindProperty("entries");
            if (entriesProperty == null || !entriesProperty.isArray)
            {
                return false;
            }

            for (int i = 0; i < entriesProperty.arraySize; i++)
            {
                SerializedProperty entry = entriesProperty.GetArrayElementAtIndex(i);
                if (entry == null)
                {
                    continue;
                }

                SerializedProperty keyProperty = entry.FindPropertyRelative("key");
                if (keyProperty == null || !string.Equals(keyProperty.stringValue, key, System.StringComparison.Ordinal))
                {
                    continue;
                }

                return TryBuildEntrySourceInfo(entry, out info, out displayObject);
            }

            return false;
        }

        private static bool TryBuildEntrySourceInfo(SerializedProperty entry, out string info, out Object displayObject)
        {
            info = null;
            displayObject = null;
            if (entry == null)
            {
                return false;
            }

            SerializedProperty valueTypeProperty = entry.FindPropertyRelative("valueType");
            if (valueTypeProperty == null)
            {
                return false;
            }

            GlobalValueType valueType = (GlobalValueType)valueTypeProperty.enumValueIndex;
            if (valueType == GlobalValueType.OutputProvider)
            {
                SerializedProperty providerProperty = entry.FindPropertyRelative("outputProvider");
                MonoBehaviour provider = providerProperty != null ? providerProperty.objectReferenceValue as MonoBehaviour : null;
                if (provider == null)
                {
                    return false;
                }

                string gameObjectName = provider.gameObject != null ? provider.gameObject.name : "None";
                info = gameObjectName + "(" + provider.GetType().Name + ")";
                displayObject = provider.gameObject != null ? provider.gameObject : provider;
                return true;
            }

            if (valueType == GlobalValueType.GameObject)
            {
                SerializedProperty objectValueProperty = entry.FindPropertyRelative("objectValue");
                Object objectValue = objectValueProperty != null ? objectValueProperty.objectReferenceValue : null;
                GameObject gameObject = objectValue as GameObject;
                if (gameObject != null)
                {
                    info = gameObject.name + "(GameObject)";
                    displayObject = gameObject;
                    return true;
                }

                Component component = objectValue as Component;
                if (component != null)
                {
                    string gameObjectName = component.gameObject != null ? component.gameObject.name : "None";
                    info = gameObjectName + "(" + component.GetType().Name + ")";
                    displayObject = component.gameObject != null ? component.gameObject : component;
                    return true;
                }
            }

            return false;
        }

        private static string[] GetGlobalKeys(System.Type expectedValueType, bool expectsList)
        {
            GlobalContext globalInstance = FindGlobalInstance();
            if (globalInstance == null)
            {
                return new string[0];
            }

            if (expectedValueType == null)
            {
                return globalInstance.GetKeys();
            }

            return globalInstance.GetKeys(expectedValueType, expectsList);
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

            int i;
            for (i = 0; i < globals.Length; i++)
            {
                GlobalContext global = globals[i];
                if (global == null)
                {
                    continue;
                }

                if (EditorUtility.IsPersistent(global))
                {
                    continue;
                }

                return global;
            }

            return globals[0];
        }

        private static bool IsListProperty(SerializedProperty property)
        {
            return property != null && property.isArray && property.propertyType != SerializedPropertyType.String;
        }

        private bool IsListFieldType()
        {
            bool isListType;
            FindSupportedValueType(fieldInfo != null ? fieldInfo.FieldType : null, out isListType);
            return isListType;
        }

        private static System.Type FindSupportedValueType(System.Type type)
        {
            if (TryResolveValueTypeFromGetMethod(type, out System.Type methodValueType, out bool methodIsListType))
            {
                return methodValueType;
            }

            while (type != null)
            {
                if (type.IsGenericType)
                {
                    System.Type genericTypeDefinition = type.GetGenericTypeDefinition();
                    if (genericTypeDefinition == typeof(MMVar<>) || genericTypeDefinition == typeof(MMListVar<>))
                    {
                        return type.GetGenericArguments()[0];
                    }
                }

                type = type.BaseType;
            }

            return null;
        }

        private static string GetSupportedDisplayTypeName(System.Type fieldType)
        {
            bool isListType;
            System.Type valueType = FindSupportedValueType(fieldType, out isListType);
            if (valueType == null)
            {
                return null;
            }

            return isListType ? "List<" + valueType.Name + ">" : valueType.Name;
        }

        private static System.Type FindSupportedValueType(System.Type type, out bool isListType)
        {
            isListType = false;
            if (TryResolveValueTypeFromGetMethod(type, out System.Type methodValueType, out bool methodIsListType))
            {
                isListType = methodIsListType;
                return methodValueType;
            }

            while (type != null)
            {
                if (type.IsGenericType)
                {
                    System.Type genericTypeDefinition = type.GetGenericTypeDefinition();
                    if (genericTypeDefinition == typeof(MMVar<>))
                    {
                        return type.GetGenericArguments()[0];
                    }

                    if (genericTypeDefinition == typeof(MMListVar<>))
                    {
                        isListType = true;
                        return type.GetGenericArguments()[0];
                    }
                }

                type = type.BaseType;
            }

            return null;
        }

        private static bool TryResolveValueTypeFromGetMethod(System.Type type, out System.Type valueType, out bool isListType)
        {
            valueType = null;
            isListType = false;
            if (type == null)
            {
                return false;
            }

            MethodInfo getter = type.GetMethod("Get", BindingFlags.Instance | BindingFlags.Public, null, System.Type.EmptyTypes, null);
            if (getter == null)
            {
                return false;
            }

            System.Type returnType = getter.ReturnType;
            if (returnType == null || returnType == typeof(void))
            {
                return false;
            }

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(List<>))
            {
                System.Type[] genericArguments = returnType.GetGenericArguments();
                if (genericArguments != null && genericArguments.Length == 1)
                {
                    valueType = genericArguments[0];
                    isListType = true;
                    return true;
                }

                return false;
            }

            valueType = returnType;
            return true;
        }

        private string GetDefaultObjectLabel()
        {
            if (fieldInfo == null || string.IsNullOrEmpty(fieldInfo.Name))
            {
                return FieldNames.Object;
            }

            return fieldInfo.Name;
        }

        private void DrawServiceField(Rect position, SerializedProperty property, System.Type expectedValueType)
        {
            Rect fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            GUIContent label = new GUIContent(GetDefaultObjectLabel());
            Service currentService = property.objectReferenceValue as Service;
            Service nextService = EditorGUI.ObjectField(fieldRect, label, currentService, typeof(Service), true) as Service;
            bool expectsList = IsListFieldType();
            if (!ReferenceEquals(nextService, currentService))
            {
                string assignError = GetServiceValidationError(nextService, expectedValueType, expectsList);
                if (nextService == null || string.IsNullOrEmpty(assignError))
                {
                    property.objectReferenceValue = nextService;
                }
            }

            string validationError = GetServiceValidationError(currentService, expectedValueType, expectsList);
            if (string.IsNullOrEmpty(validationError))
            {
                return;
            }

            float helpY = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            float helpHeight = GetHelpBoxHeight(validationError);
            Rect helpRect = new Rect(position.x, helpY, position.width, helpHeight);
            EditorGUI.HelpBox(helpRect, validationError, MessageType.Error);
        }

        private static string GetServiceValidationError(SerializedProperty property, System.Type expectedValueType)
        {
            if (property == null)
            {
                return null;
            }

            Service selectedService = property.objectReferenceValue as Service;
            if (selectedService == null)
            {
                return null;
            }

            return GetServiceValidationError(selectedService, expectedValueType, false);
        }

        private static string GetServiceValidationError(Service service, System.Type expectedValueType, bool expectsList)
        {
            if (service == null)
            {
                return null;
            }

            if (!expectsList)
            {
                string error;
                if (ServiceOutputUtility.TryValidateService(service, expectedValueType, out error))
                {
                    return null;
                }

                return error;
            }

            if (expectedValueType == null)
            {
                return null;
            }

            System.Type outputType = GetServiceOutputType(service);
            if (outputType == null)
            {
                return service.GetType().Name + " has no valid [Output] field.";
            }

            if (ServiceOutputUtility.IsListOutputCompatible(outputType, expectedValueType))
            {
                return null;
            }

            return service.GetType().Name + " output type " + outputType.Name + " is not compatible with " + expectedValueType.Name + " list.";
        }

        private static System.Type GetServiceOutputType(Service service)
        {
            if (service == null)
            {
                return null;
            }

            FieldInfo outputField;
            string error;
            if (!ServiceOutputUtility.TryGetOutputField(service.GetType(), out outputField, out error))
            {
                return null;
            }

            return outputField != null ? outputField.FieldType : null;
        }

        private static float GetHelpBoxHeight(string message)
        {
            return EditorStyles.helpBox.CalcHeight(new GUIContent(message), EditorGUIUtility.currentViewWidth - 40f);
        }

        private void DrawDefaultObjectField(Rect position, SerializedProperty property, System.Type expectedValueType)
        {
            GUIContent label = new GUIContent(GetDefaultObjectLabel());
            if (IsListProperty(property))
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            if (expectedValueType == null)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            if (expectedValueType == typeof(GameObject))
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            Object currentObject = GetDisplayedDefaultObject(property, expectedValueType);
            Object nextObject = EditorGUI.ObjectField(position, label, currentObject, expectedValueType, true);
            if (ReferenceEquals(nextObject, currentObject))
            {
                return;
            }

            property.objectReferenceValue = ExtractAssignedGameObject(nextObject);
        }

        private static Object GetDisplayedDefaultObject(SerializedProperty property, System.Type expectedValueType)
        {
            GameObject storedGameObject = property.objectReferenceValue as GameObject;
            if (storedGameObject == null || expectedValueType == null)
            {
                return null;
            }

            if (expectedValueType == typeof(GameObject))
            {
                return storedGameObject;
            }

            if (typeof(Component).IsAssignableFrom(expectedValueType))
            {
                return storedGameObject.GetComponent(expectedValueType);
            }

            return storedGameObject;
        }

        private static GameObject ExtractAssignedGameObject(Object value)
        {
            if (value == null)
            {
                return null;
            }

            GameObject gameObject = value as GameObject;
            if (gameObject != null)
            {
                return gameObject;
            }

            Component component = value as Component;
            if (component != null)
            {
                return component.gameObject;
            }

            return null;
        }
    }

    [CustomPropertyDrawer(typeof(MMListVar), true)]
    public class MMListVarDrawer : MMVarDrawer
    {
    }
}
#endif
