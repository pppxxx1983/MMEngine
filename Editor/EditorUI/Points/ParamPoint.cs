using System.Collections.Generic;
using System.Reflection;
using SP;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public sealed class ParamPoint : LinkPoint
    {
        private SerializedObject serializedObject;
        private SerializedProperty rootProperty;
        private readonly VisualElement fieldRoot;
        private string typePropertyPath;
        private PropertyField propertyField;

        public System.Type ValueType { get; private set; }

        public ParamPoint() : base(LinkPointType.Input)
        {
            style.alignSelf = Align.Stretch;
            style.flexGrow = 1f;
            style.flexShrink = 1f;
            style.marginBottom = 2f;

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
            List<InputType> allowedTypes = GetAllowedInputTypes(fieldType);
            if (allowedTypes.Count == 0)
            {
                return null;
            }

            List<string> choices = new List<string>(allowedTypes.Count);
            for (int i = 0; i < allowedTypes.Count; i++)
            {
                choices.Add(allowedTypes[i].ToString());
            }

            InputType currentType = GetResolvedInputType(fieldType, typeProperty);
            string currentChoice = currentType.ToString();
            PopupField<string> popupField = new PopupField<string>(choices, currentChoice);
            popupField.RegisterValueChangedCallback(evt =>
            {
                InputType nextType;
                if (!System.Enum.TryParse(evt.newValue, out nextType))
                {
                    return;
                }

                typeProperty.enumValueIndex = (int)nextType;
                serializedObject.ApplyModifiedProperties();
                RebuildVarValueField(fieldType);
                UIManager.Instance.VarLine?.MarkDirtyRepaint();
            });
            return popupField;
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
            fieldRoot.Add(valueField);
            valueField.Bind(serializedObject);
        }

        private SerializedProperty GetVarValueProperty(System.Type fieldType)
        {
            SerializedProperty typeProperty = rootProperty.FindPropertyRelative("type");
            if (typeProperty == null)
            {
                return null;
            }

            InputType inputType = GetResolvedInputType(fieldType, typeProperty);
            if (inputType == InputType.Service)
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
            popupField.RegisterValueChangedCallback(evt =>
            {
                property.stringValue = evt.newValue == "<No Global>" ? string.Empty : evt.newValue;
                serializedObject.ApplyModifiedProperties();
                UIManager.Instance.VarLine?.MarkDirtyRepaint();
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
            UIManager.Instance.VarLine?.MarkDirtyRepaint();
        }
    }
}
