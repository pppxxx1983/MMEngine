using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SP;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public sealed class ParamPoint : LinkPoint
    {
        private Service boundService;
        private FieldInfo boundFieldInfo;
        private SerializedObject serializedObject;
        private SerializedProperty rootProperty;
        private readonly VisualElement fieldRoot;
        private string typePropertyPath;
        private PropertyField propertyField;

        public ParamPoint() : base(LinkPointType.Input)
        {
            style.alignSelf = Align.Stretch;
            style.flexGrow = 1f;
            style.flexShrink = 1f;
            style.marginBottom = 2f;
            style.minHeight = 18f;

            fieldRoot = new HLayout();
            fieldRoot.style.flexGrow = 1f;
            fieldRoot.style.flexShrink = 1f;
            fieldRoot.style.justifyContent = Justify.FlexStart;
            fieldRoot.style.alignItems = Align.Center;
            fieldRoot.style.marginLeft = 6f;
            Add(fieldRoot);
            SetMirror(false);
            RegisterCallback<SerializedPropertyChangeEvent>(OnSerializedPropertyChange);
        }

        public void SetValueType(System.Type valueType)
        {
            ValueType = valueType;
        }

        public void Setup(Service service, FieldInfo fieldInfo)
        {
            boundService = service;
            boundFieldInfo = fieldInfo;
            fieldRoot.Clear();
            serializedObject = null;
            rootProperty = null;
            typePropertyPath = null;
            propertyField = null;
            SetValueType(fieldInfo != null ? fieldInfo.FieldType : null);
            if (service == null || fieldInfo == null)
            {
                return;
            }

            FieldName = fieldInfo.Name;
            NodeId = SceneNodeFactory.GetSceneNodeId(service.gameObject);

            serializedObject = new SerializedObject(service);
            rootProperty = serializedObject.FindProperty(fieldInfo.Name);
            if (rootProperty == null)
            {
                return;
            }

            if (typeof(MMVar).IsAssignableFrom(fieldInfo.FieldType) || typeof(MMListVar).IsAssignableFrom(fieldInfo.FieldType))
            {
                NormalizeVarInputType(fieldInfo.FieldType);
                ValueType = GetVarValueType(fieldInfo.FieldType);
                ExpectsList = fieldInfo.FieldType != null && typeof(MMListVar).IsAssignableFrom(fieldInfo.FieldType);
                BuildVarFields(fieldInfo);
                return;
            }

            ValueType = fieldInfo.FieldType;
            ExpectsList = fieldInfo.FieldType != null && fieldInfo.FieldType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(fieldInfo.FieldType);

            propertyField = new PropertyField(rootProperty);
            propertyField.label = ObjectNames.NicifyVariableName(fieldInfo.Name);
            propertyField.style.flexGrow = 1f;
            propertyField.style.flexShrink = 1f;
            fieldRoot.Add(propertyField);
            propertyField.Bind(serializedObject);
        }

        public void RefreshBinding()
        {
            if (boundService == null || boundFieldInfo == null)
            {
                return;
            }

            Setup(boundService, boundFieldInfo);
        }

        public string GetBindingSignature()
        {
            if (boundService == null || boundFieldInfo == null)
            {
                return string.Empty;
            }

            SerializedObject latestObject = new SerializedObject(boundService);
            SerializedProperty latestRootProperty = latestObject.FindProperty(boundFieldInfo.Name);
            if (latestRootProperty == null)
            {
                return boundFieldInfo.Name + "|missing";
            }

            StringBuilder builder = new StringBuilder();
            builder.Append(boundFieldInfo.Name);
            builder.Append('|');
            builder.Append(boundFieldInfo.FieldType.AssemblyQualifiedName);

            if (typeof(MMVar).IsAssignableFrom(boundFieldInfo.FieldType) || typeof(MMListVar).IsAssignableFrom(boundFieldInfo.FieldType))
            {
                AppendPropertyValue(builder, latestRootProperty.FindPropertyRelative("type"));
                AppendPropertyValue(builder, latestRootProperty.FindPropertyRelative("service"));
                AppendPropertyValue(builder, latestRootProperty.FindPropertyRelative("global"));
                AppendPropertyValue(builder, latestRootProperty.FindPropertyRelative("obj"));
                AppendPropertyValue(builder, latestRootProperty.FindPropertyRelative("objs"));
            }
            else
            {
                AppendPropertyValue(builder, latestRootProperty);
            }

            return builder.ToString();
        }

        public void SetMirror(bool mirror)
        {
            if (!mirror)
            {
                style.flexDirection = FlexDirection.Row;
                SetReverseOrder(true);
                fieldRoot.style.marginLeft = 6f;
                fieldRoot.style.marginRight = 0f;
                return;
            }

            style.flexDirection = FlexDirection.RowReverse;
            SetReverseOrder(true);
            fieldRoot.style.marginLeft = 0f;
            fieldRoot.style.marginRight = 6f;
        }

        private void BuildVarFields(FieldInfo fieldInfo)
        {
            SerializedProperty typeProperty = rootProperty.FindPropertyRelative("type");
            if (typeProperty == null)
            {
                return;
            }

            typePropertyPath = typeProperty.propertyPath;

            VisualElement typeField = BuildInputTypeField(fieldInfo.FieldType, typeProperty);
            if (typeField != null)
            {
                typeField.style.width = 90f;
                typeField.style.flexShrink = 0f;
                typeField.style.marginRight = 6f;
                fieldRoot.Add(typeField);
            }

            RebuildVarValueField(fieldInfo.FieldType);
        }

        private VisualElement BuildInputTypeField(System.Type fieldType, SerializedProperty typeProperty)
        {
            string displayText = BuildValueTypeDisplayText(fieldType);
            if (string.IsNullOrEmpty(displayText))
            {
                return null;
            }

            Label typeLabel = new Label(displayText);
            typeLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            typeLabel.style.color = new Color(0.78f, 0.82f, 0.88f, 1f);
            typeLabel.style.fontSize = 9f;
            typeLabel.style.minHeight = 18f;
            typeLabel.style.whiteSpace = WhiteSpace.NoWrap;
            typeLabel.style.overflow = Overflow.Hidden;
            typeLabel.style.textOverflow = TextOverflow.Ellipsis;
            return typeLabel;
        }

        private void RebuildVarValueField(System.Type fieldType)
        {
            if (rootProperty == null || serializedObject == null)
            {
                return;
            }

            while (fieldRoot.childCount > 1)
            {
                fieldRoot.RemoveAt(1);
            }

            SerializedProperty valueProperty = GetVarValueProperty(fieldType);
            if (valueProperty == null)
            {
                return;
            }

            if (IsOutputProperty(valueProperty))
            {
                VisualElement outputField = BuildOutputProviderField(valueProperty, fieldType);
                if (outputField != null)
                {
                    fieldRoot.Add(outputField);
                }

                return;
            }

            if (IsGlobalProperty(valueProperty))
            {
                VisualElement globalField = BuildGlobalKeyField(valueProperty, fieldType);
                if (globalField != null)
                {
                    fieldRoot.Add(globalField);
                }

                return;
            }

            PropertyField valueField = new PropertyField(valueProperty);
            valueField.label = string.Empty;
            valueField.style.flexGrow = 1f;
            valueField.style.flexShrink = 1f;
            valueField.style.minHeight = 18f;
            fieldRoot.Add(valueField);
            valueField.Bind(serializedObject);
            CompactField(valueField);
        }

        private SerializedProperty GetVarValueProperty(System.Type fieldType)
        {
            SerializedProperty typeProperty = rootProperty.FindPropertyRelative("type");
            if (typeProperty == null)
            {
                return null;
            }

            InputType inputType = GetResolvedInputType(fieldType, typeProperty);
            if (inputType == InputType.Output)
            {
                return rootProperty.FindPropertyRelative("service");
            }

            if (inputType == InputType.Global)
            {
                return rootProperty.FindPropertyRelative("global");
            }

            if (fieldType != null && typeof(MMListVar).IsAssignableFrom(fieldType))
            {
                return rootProperty.FindPropertyRelative("objs");
            }

            return rootProperty.FindPropertyRelative("obj");
        }

        private void NormalizeVarInputType(System.Type fieldType)
        {
            if (rootProperty == null || serializedObject == null)
            {
                return;
            }

            SerializedProperty typeProperty = rootProperty.FindPropertyRelative("type");
            if (typeProperty == null)
            {
                return;
            }

            InputType currentType = (InputType)typeProperty.enumValueIndex;
            if (IsInputTypeSupported(fieldType, currentType))
            {
                return;
            }

            typeProperty.enumValueIndex = (int)GetFallbackInputType(fieldType);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool IsGlobalProperty(SerializedProperty property)
        {
            return property != null && property.name == "global";
        }

        private static bool IsOutputProperty(SerializedProperty property)
        {
            return property != null && property.name == "service";
        }

        private VisualElement BuildOutputProviderField(SerializedProperty property, System.Type fieldType)
        {
            System.Type valueType = GetVarValueType(fieldType);
            bool expectsList = fieldType != null && typeof(MMListVar).IsAssignableFrom(fieldType);

            ObjectField objectField = new ObjectField();
            objectField.objectType = typeof(UnityEngine.Object);
            objectField.allowSceneObjects = true;
            objectField.style.flexGrow = 1f;
            objectField.style.flexShrink = 1f;
            objectField.style.minHeight = 18f;
            objectField.value = property.objectReferenceValue;
            objectField.RegisterValueChangedCallback(evt =>
            {
                MonoBehaviour currentProvider = property.objectReferenceValue as MonoBehaviour;
                if (evt.newValue == null)
                {
                    property.objectReferenceValue = null;
                    serializedObject.ApplyModifiedProperties();
                    objectField.SetValueWithoutNotify(null);
                    RefreshNodePresentation();
                    return;
                }

                MonoBehaviour resolvedProvider = ResolveOutputProviderSelection(evt.newValue, valueType, expectsList);
                if (resolvedProvider == null)
                {
                    objectField.SetValueWithoutNotify(currentProvider);
                    return;
                }

                property.objectReferenceValue = resolvedProvider;
                serializedObject.ApplyModifiedProperties();
                objectField.SetValueWithoutNotify(resolvedProvider);
                RefreshNodePresentation();
            });

            return objectField;
        }

        private VisualElement BuildGlobalKeyField(SerializedProperty property, System.Type fieldType)
        {
            System.Type valueType = GetVarValueType(fieldType);
            bool expectsList = fieldType != null && typeof(MMListVar).IsAssignableFrom(fieldType);
            string[] keys = GlobalContext.ins != null && valueType != null
                ? GlobalContext.ins.GetKeys(valueType, expectsList)
                : new string[0];

            List<string> choices = new List<string>();
            choices.Add("<No Global>");
            for (int i = 0; i < keys.Length; i++)
            {
                choices.Add(keys[i]);
            }

            string currentValue = string.IsNullOrEmpty(property.stringValue) ? "<No Global>" : property.stringValue;
            if (!choices.Contains(currentValue))
            {
                choices.Add(currentValue);
            }

            PopupField<string> popupField = new PopupField<string>(choices, currentValue);
            popupField.style.flexGrow = 1f;
            popupField.style.flexShrink = 1f;
            popupField.style.minHeight = 18f;
            popupField.style.fontSize = 10f;
            popupField.RegisterValueChangedCallback(evt =>
            {
                property.stringValue = evt.newValue == "<No Global>" ? string.Empty : evt.newValue;
                serializedObject.ApplyModifiedProperties();
                RefreshNodePresentation();
            });
            return popupField;
        }

        private static System.Type GetVarValueType(System.Type fieldType)
        {
            if (TryResolveValueTypeFromGetMethod(fieldType, out System.Type resolvedType))
            {
                return resolvedType;
            }

            System.Type current = fieldType;
            while (current != null)
            {
                if (current.IsGenericType)
                {
                    System.Type genericType = current.GetGenericTypeDefinition();
                    if (genericType == typeof(MMVar<>) || genericType == typeof(MMListVar<>))
                    {
                        System.Type[] arguments = current.GetGenericArguments();
                        if (arguments != null && arguments.Length == 1)
                        {
                            return arguments[0];
                        }
                    }
                }

                current = current.BaseType;
            }

            return null;
        }

        private static bool TryResolveValueTypeFromGetMethod(System.Type fieldType, out System.Type valueType)
        {
            valueType = null;
            if (fieldType == null)
            {
                return false;
            }

            MethodInfo getter = fieldType.GetMethod("Get", BindingFlags.Instance | BindingFlags.Public, null, System.Type.EmptyTypes, null);
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
                System.Type[] arguments = returnType.GetGenericArguments();
                if (arguments != null && arguments.Length == 1)
                {
                    valueType = arguments[0];
                    return true;
                }

                return false;
            }

            valueType = returnType;
            return true;
        }

        private static InputType GetResolvedInputType(System.Type fieldType, SerializedProperty typeProperty)
        {
            if (typeProperty == null)
            {
                return InputType.Default;
            }

            InputType currentType = (InputType)typeProperty.enumValueIndex;
            if (IsInputTypeSupported(fieldType, currentType))
            {
                return currentType;
            }

            return GetFallbackInputType(fieldType);
        }

        private static bool IsInputTypeSupported(System.Type fieldType, InputType inputType)
        {
            object instance = CreateVarInstance(fieldType);
            MMVar mmVar = instance as MMVar;
            if (mmVar != null)
            {
                return mmVar.IsInputTypeSupported(inputType);
            }

            MMListVar mmListVar = instance as MMListVar;
            if (mmListVar != null)
            {
                return mmListVar.IsInputTypeSupported(inputType);
            }

            return true;
        }

        private static InputType GetFallbackInputType(System.Type fieldType)
        {
            object instance = CreateVarInstance(fieldType);
            MMVar mmVar = instance as MMVar;
            if (mmVar != null)
            {
                return mmVar.GetFallbackInputType();
            }

            MMListVar mmListVar = instance as MMListVar;
            if (mmListVar != null)
            {
                return mmListVar.GetFallbackInputType();
            }

            return InputType.Default;
        }

        private static List<InputType> GetAllowedInputTypes(System.Type fieldType)
        {
            List<InputType> allowed = new List<InputType>();
            InputType[] allTypes = (InputType[])System.Enum.GetValues(typeof(InputType));
            for (int i = 0; i < allTypes.Length; i++)
            {
                if (IsInputTypeSupported(fieldType, allTypes[i]))
                {
                    allowed.Add(allTypes[i]);
                }
            }

            if (allowed.Count == 0)
            {
                allowed.Add(InputType.Default);
            }

            return allowed;
        }

        private string BuildValueTypeDisplayText(System.Type fieldType)
        {
            System.Type valueType = GetVarValueType(fieldType);
            if (valueType == null)
            {
                return "Value";
            }

            string typeName = ObjectNames.NicifyVariableName(valueType.Name);
            if (fieldType != null && typeof(MMListVar).IsAssignableFrom(fieldType))
            {
                return "List<" + typeName + ">";
            }

            return typeName;
        }

        private static object CreateVarInstance(System.Type fieldType)
        {
            if (fieldType == null)
            {
                return null;
            }

            try
            {
                return System.Activator.CreateInstance(fieldType);
            }
            catch
            {
                return null;
            }
        }

        private static MonoBehaviour ResolveOutputProviderSelection(UnityEngine.Object pickedObject, System.Type expectedValueType, bool expectsList)
        {
            if (pickedObject == null)
            {
                return null;
            }

            if (pickedObject is MonoBehaviour directProvider && IsOutputProviderCompatible(directProvider, expectedValueType, expectsList))
            {
                return directProvider;
            }

            GameObject owner = null;
            if (pickedObject is GameObject pickedGameObject)
            {
                owner = pickedGameObject;
            }
            else if (pickedObject is Component pickedComponent)
            {
                owner = pickedComponent.gameObject;
            }

            if (owner == null)
            {
                return null;
            }

            MonoBehaviour[] providers = owner.GetComponents<MonoBehaviour>();
            for (int i = 0; i < providers.Length; i++)
            {
                MonoBehaviour provider = providers[i];
                if (provider != null && IsOutputProviderCompatible(provider, expectedValueType, expectsList))
                {
                    return provider;
                }
            }

            return null;
        }

        private static bool IsOutputProviderCompatible(MonoBehaviour provider, System.Type expectedValueType, bool expectsList)
        {
            if (provider == null)
            {
                return false;
            }

            if (!expectsList)
            {
                return OutputUtility.TryValidateOutputProvider(provider, expectedValueType, out _);
            }

            if (!OutputUtility.TryGetOutputField(provider.GetType(), out FieldInfo outputField, out _ ) || outputField == null)
            {
                return false;
            }

            return OutputUtility.IsListOutputCompatible(outputField.FieldType, expectedValueType);
        }

        private void OnSerializedPropertyChange(SerializedPropertyChangeEvent evt)
        {
            if (evt == null || evt.changedProperty == null || string.IsNullOrEmpty(typePropertyPath))
            {
                return;
            }

            if (evt.changedProperty.propertyPath != typePropertyPath)
            {
                return;
            }

            if (ValueType == null)
            {
                return;
            }

            RebuildVarValueField(ValueType);
            RefreshNodePresentation();
        }

        private void RefreshNodePresentation()
        {
            UINode node = this.GetFirstAncestorOfType<UINode>();
            if (node != null)
            {
                node.Refresh();
            }

            UIManager.Instance.VarLine?.MarkDirtyRepaint();
            UIManager.Instance.Curve?.MarkDirtyRepaint();
        }

        private static void CompactField(VisualElement field)
        {
            if (field == null)
            {
                return;
            }

            field.schedule.Execute(() =>
            {
                ApplyCompactStylesRecursive(field);
            });
        }

        private static void ApplyCompactStylesRecursive(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            if (element is Label label)
            {
                label.style.fontSize = 9f;
                label.style.whiteSpace = WhiteSpace.NoWrap;
                label.style.overflow = Overflow.Hidden;
                label.style.textOverflow = TextOverflow.Ellipsis;
            }

            element.style.minHeight = 18f;
            element.style.height = StyleKeyword.Auto;

            int childCount = element.childCount;
            for (int i = 0; i < childCount; i++)
            {
                ApplyCompactStylesRecursive(element[i]);
            }
        }

        private static void AppendPropertyValue(StringBuilder builder, SerializedProperty property)
        {
            builder.Append('|');
            if (property == null)
            {
                builder.Append("<null>");
                return;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    Object reference = property.objectReferenceValue;
                    builder.Append(reference != null ? reference.GetInstanceID().ToString() : "0");
                    break;
                case SerializedPropertyType.String:
                    builder.Append(property.stringValue ?? string.Empty);
                    break;
                case SerializedPropertyType.Enum:
                    builder.Append(property.enumValueIndex);
                    break;
                case SerializedPropertyType.Integer:
                    builder.Append(property.intValue);
                    break;
                case SerializedPropertyType.Boolean:
                    builder.Append(property.boolValue ? "1" : "0");
                    break;
                case SerializedPropertyType.Generic:
                    if (property.isArray)
                    {
                        builder.Append(property.arraySize);
                        for (int i = 0; i < property.arraySize; i++)
                        {
                            SerializedProperty element = property.GetArrayElementAtIndex(i);
                            if (element == null || element.propertyType != SerializedPropertyType.ObjectReference)
                            {
                                continue;
                            }

                            Object referenceElement = element.objectReferenceValue;
                            builder.Append(',');
                            builder.Append(referenceElement != null ? referenceElement.GetInstanceID().ToString() : "0");
                        }
                    }
                    else
                    {
                        builder.Append(property.contentHash.ToString());
                    }
                    break;
                default:
                    builder.Append(property.contentHash.ToString());
                    break;
            }
        }
    }
}

