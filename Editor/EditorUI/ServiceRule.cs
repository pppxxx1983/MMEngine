using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using SP;
using UnityEngine;
using System;
using UnityEditor;

namespace PlayableFramework.Editor
{
    public sealed class ServiceRule
    {
        private static ServiceRule instance;

        public static ServiceRule Instance => instance ??= new ServiceRule();

        private ServiceRule()
        {
        }

        public Service GetService(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return null;
            }

            GameObject nodeObject;
            if (!GameObjectOperator.TryGetNodeObject(nodeId, out nodeObject) || nodeObject == null)
            {
                return null;
            }

            return nodeObject.GetComponent<Service>();
        }

        public void GetFlowPorts(string nodeId, out bool hasEnterPort, out bool hasNextPort)
        {
            hasEnterPort = true;
            hasNextPort = true;

            Service service = GetService(nodeId);
            if (service is not IFlowPort flowPort)
            {
                return;
            }

            hasEnterPort = flowPort.HasEnterPort;
            hasNextPort = flowPort.HasNextPort;
        }

        public bool HasMirror(string nodeId)
        {
            return GetService(nodeId) is IMirrorNode;
        }

        public bool GetMirror(string nodeId)
        {
            Service service = GetService(nodeId);
            if (service is not IMirrorNode mirrorNode)
            {
                return false;
            }

            return mirrorNode.IsMirror;
        }

        public bool ToggleMirror(string nodeId)
        {
            Service service = GetService(nodeId);
            if (service is not IMirrorNode mirrorNode)
            {
                return false;
            }

            UnityEditor.Undo.RecordObject(service, "Toggle Mirror");
            mirrorNode.IsMirror = !mirrorNode.IsMirror;
            UnityEditor.EditorUtility.SetDirty(service);
            return mirrorNode.IsMirror;
        }

        public List<FieldInfo> GetInputFields(string nodeId)
        {
            List<FieldInfo> inputFields = new List<FieldInfo>();
            Service service = GetService(nodeId);
            if (service == null)
            {
                return inputFields;
            }

            FieldInfo[] fields = service.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].IsDefined(typeof(InputAttribute), true))
                {
                    inputFields.Add(fields[i]);
                }
            }

            return inputFields;
        }

        public List<FieldInfo> GetOutputFields(string nodeId)
        {
            List<FieldInfo> outputFields = new List<FieldInfo>();
            Service service = GetService(nodeId);
            if (service == null)
            {
                return outputFields;
            }

            FieldInfo[] fields = service.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].IsDefined(typeof(OutputAttribute), true))
                {
                    outputFields.Add(fields[i]);
                }
            }

            return outputFields;
        }

        public bool CanConnectValue(LinkPoint outputPoint, LinkPoint inputPoint)
        {
            if (outputPoint == null || inputPoint == null)
            {
                return false;
            }

            MonoBehaviour outputService;
            FieldInfo outputField;
            Type outputFieldType;
            bool outputIsList;
            if (!TryGetOutputBinding(outputPoint, out outputService, out outputField, out outputFieldType, out outputIsList))
            {
                return false;
            }

            Service inputService;
            FieldInfo inputField;
            Type expectedType;
            bool expectsList;
            bool isVarInput;
            if (!TryGetInputBinding(inputPoint, out inputService, out inputField, out expectedType, out expectsList, out isVarInput))
            {
                return false;
            }

            if (isVarInput)
            {
                if (expectsList)
                {
                    return OutputUtility.IsListOutputCompatible(outputFieldType, expectedType);
                }

                return OutputUtility.IsOutputCompatible(outputFieldType, expectedType);
            }

            if (expectsList)
            {
                return OutputUtility.IsListOutputCompatible(outputFieldType, expectedType);
            }

            return outputFieldType != null && expectedType != null && (expectedType.IsAssignableFrom(outputFieldType) || OutputUtility.IsOutputCompatible(outputFieldType, expectedType));
        }

        public bool TryApplyValue(LinkPoint outputPoint, LinkPoint inputPoint)
        {
            if (outputPoint == null || inputPoint == null)
            {
                return false;
            }

            MonoBehaviour outputService;
            FieldInfo outputField;
            Type outputFieldType;
            bool outputIsList;
            if (!TryGetOutputBinding(outputPoint, out outputService, out outputField, out outputFieldType, out outputIsList))
            {
                return false;
            }

            Service inputService;
            FieldInfo inputField;
            Type expectedType;
            bool expectsList;
            bool isVarInput;
            if (!TryGetInputBinding(inputPoint, out inputService, out inputField, out expectedType, out expectsList, out isVarInput))
            {
                return false;
            }

            if (isVarInput)
            {
                return BindInputVarToService(inputService, inputField, expectsList, outputService);
            }

            return AssignOutputValueToInput(inputService, inputField, expectedType, expectsList, outputService);
        }

        public bool TryApplyFlow(string nextNodeId, string enterNodeId)
        {
            return false;
        }

        public bool TryClearFlowLink(string parentNodeId, string childNodeId)
        {
            return false;
        }

        public bool TryGetGuideEnterIds(string nodeId, out List<string> enterIds)
        {
            enterIds = null;
            return false;
        }

        public bool TryGetGuideNextIds(string nodeId, out List<string> nextIds)
        {
            nextIds = null;
            return false;
        }

        /// <summary>
        /// RefEnter 连接到 Next，保存 RefEnter 节点 id 到目标 Service 的 IRefPortID 字段。
        /// </summary>
        public bool TryApplyRefNextToEnter(string refNextNodeId, string enterNodeId)
        {
            if (string.IsNullOrEmpty(refNextNodeId) || string.IsNullOrEmpty(enterNodeId))
            {
                return false;
            }

            Service refService = GetService(refNextNodeId);
            if (refService == null || refService is not IRefPort refPort)
            {
                return false;
            }

            Undo.RecordObject(refService, "Link RefNext to Enter");
            refPort.EnterId = enterNodeId;
            EditorUtility.SetDirty(refService);
            return true;
        }

        public bool TryApplyRefEnterToNext(string refEnterNodeId, string nextNodeId)
        {
            if (string.IsNullOrEmpty(refEnterNodeId) || string.IsNullOrEmpty(nextNodeId))
            {
                return false;
            }

            // 1. 保存 NextId 到 RefEnter 所在 Service 的 IRefPort.NextId
            Service refService = GetService(refEnterNodeId);
            if (refService == null || refService is not IRefPort refPort)
            {
                return false;
            }

            // 2. 保存 IRefPort id 到目标 Service 的 IRefNextID 字段（运行时逻辑用）
            Service targetService = GetService(nextNodeId);
            if (targetService == null)
            {
                return false;
            }

            System.Reflection.FieldInfo field = targetService.GetType().GetField("IRefNextID", 
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.NonPublic);

            if (field == null)
            {
                Debug.LogWarning($"[{nameof(ServiceRule)}] Target service does not have IRefNextID field: {targetService.GetType().Name}");
                return false;
            }

            Undo.RecordObject(refService, "Link RefEnter to Next");
            refPort.NextId = nextNodeId;
            EditorUtility.SetDirty(refService);

            Undo.RecordObject(targetService, "Link RefEnter to Next");
            field.SetValue(targetService, refEnterNodeId);
            EditorUtility.SetDirty(targetService);
            return true;
        }

        /// <summary>
        /// 清除 Ref 链接。
        /// </summary>
        public bool TryClearRefLink(string parentId, string childId)
        {
            if (string.IsNullOrEmpty(parentId) || string.IsNullOrEmpty(childId))
            {
                return false;
            }

            // 检查是否是 RefNext→Enter 链接（parent 是 RefNext，child 是 Enter）
            Service parentService = GetService(parentId);
            if (parentService is IRefPort parentRefPort && parentRefPort.EnterId == childId)
            {
                Undo.RecordObject(parentService, "Clear RefNext Link");
                parentRefPort.EnterId = null;
                EditorUtility.SetDirty(parentService);
                return true;
            }

            // 检查是否是 RefEnter→Next 链接（parent 是 RefEnter，child 是 Next）
            // 基于 IRefPort.NextId 判断
            if (parentService is IRefPort parentRefPort2 && parentRefPort2.NextId == childId)
            {
                Undo.RecordObject(parentService, "Clear RefEnter Link");
                parentRefPort2.NextId = null;
                EditorUtility.SetDirty(parentService);
                
                // 同时清除目标 Service 的 IRefNextID
                Service childService = GetService(childId);
                if (childService != null)
                {
                    System.Reflection.FieldInfo field = childService.GetType().GetField("IRefNextID",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic);

                    if (field != null)
                    {
                        string currentValue = field.GetValue(childService) as string;
                        if (currentValue == parentId)
                        {
                            Undo.RecordObject(childService, "Clear RefEnter Link");
                            field.SetValue(childService, null);
                            EditorUtility.SetDirty(childService);
                        }
                    }
                }
                return true;
            }

            return false;
        }

        public bool TryClearInputValue(string inputNodeId, string inputFieldName)
        {
            if (string.IsNullOrEmpty(inputNodeId) || string.IsNullOrEmpty(inputFieldName))
            {
                return false;
            }

            Service inputService = GetService(inputNodeId);
            if (inputService == null)
            {
                return false;
            }

            FieldInfo inputField = GetField(inputService.GetType(), inputFieldName);
            if (inputField == null)
            {
                return false;
            }

            return ClearInputValue(inputService, inputField);
        }

        public bool TryGetBoundOutputNodeId(LinkPoint inputPoint, out string outputNodeId)
        {
            outputNodeId = null;
            if (inputPoint == null)
            {
                return false;
            }

            Service inputService;
            FieldInfo inputField;
            Type expectedType;
            bool expectsList;
            bool isVarInput;
            if (!TryGetInputBinding(inputPoint, out inputService, out inputField, out expectedType, out expectsList, out isVarInput) || !isVarInput)
            {
                return false;
            }

            object inputVarObject = inputField.GetValue(inputService);
            MMVar singleVar = inputVarObject as MMVar;
            if (singleVar != null && singleVar.type == InputType.Output && singleVar.service != null)
            {
                outputNodeId = SceneNodeFactory.GetSceneNodeId(singleVar.service.gameObject);
                return !string.IsNullOrEmpty(outputNodeId);
            }

            MMListVar listVar = inputVarObject as MMListVar;
            if (listVar != null && listVar.type == InputType.Output && listVar.service != null)
            {
                outputNodeId = SceneNodeFactory.GetSceneNodeId(listVar.service.gameObject);
                return !string.IsNullOrEmpty(outputNodeId);
            }

            return false;
        }

        public bool TryGetBoundOutput(LinkPoint inputPoint, out string outputNodeId, out string outputFieldName)
        {
            outputNodeId = null;
            outputFieldName = null;
            if (inputPoint == null)
            {
                return false;
            }

            if (TryGetServiceBoundOutput(inputPoint, out outputNodeId, out outputFieldName))
            {
                return true;
            }

            return TryGetAssignedOutput(inputPoint, out outputNodeId, out outputFieldName);
        }

        private bool TryGetServiceBoundOutput(LinkPoint inputPoint, out string outputNodeId, out string outputFieldName)
        {
            outputNodeId = null;
            outputFieldName = null;
            Service inputService;
            FieldInfo inputField;
            Type expectedType;
            bool expectsList;
            bool isVarInput;
            if (!TryGetInputBinding(inputPoint, out inputService, out inputField, out expectedType, out expectsList, out isVarInput) || !isVarInput)
            {
                return false;
            }

            object inputVarObject = inputField.GetValue(inputService);
            MonoBehaviour boundService = null;
            if (inputVarObject is MMVar singleVar && singleVar.type == InputType.Output)
            {
                boundService = singleVar.service;
            }
            else if (inputVarObject is MMListVar listVar && listVar.type == InputType.Output)
            {
                boundService = listVar.service;
            }

            if (boundService == null)
            {
                return false;
            }

            outputNodeId = SceneNodeFactory.GetSceneNodeId(boundService.gameObject);
            List<FieldInfo> outputFields = GetOutputFields(outputNodeId);
            outputFieldName = outputFields.Count > 0 ? outputFields[0].Name : null;
            return !string.IsNullOrEmpty(outputNodeId) && !string.IsNullOrEmpty(outputFieldName);
        }

        private bool TryGetAssignedOutput(LinkPoint inputPoint, out string outputNodeId, out string outputFieldName)
        {
            outputNodeId = null;
            outputFieldName = null;
            Service inputService;
            FieldInfo inputField;
            Type expectedType;
            bool expectsList;
            bool isVarInput;
            if (!TryGetInputBinding(inputPoint, out inputService, out inputField, out expectedType, out expectsList, out isVarInput))
            {
                return false;
            }

            object inputValue = inputField.GetValue(inputService);
            if (inputValue == null || isVarInput)
            {
                return false;
            }

            IReadOnlyList<UINode> nodes = NodeManager.Instance.UINodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                UINode node = nodes[i];
                if (node == null || node.Data == null || string.IsNullOrEmpty(node.Data.Id) || node.Data.Id == inputPoint.NodeId)
                {
                    continue;
                }

                Service outputService = GetService(node.Data.Id);
                if (outputService == null)
                {
                    continue;
                }

                List<FieldInfo> outputFields = GetOutputFields(node.Data.Id);
                for (int j = 0; j < outputFields.Count; j++)
                {
                    FieldInfo outputField = outputFields[j];
                    if (outputField == null)
                    {
                        continue;
                    }

                    object outputValue = outputField.GetValue(outputService);
                    if (!AreAssignedValuesEqual(inputValue, outputValue))
                    {
                        continue;
                    }

                    outputNodeId = node.Data.Id;
                    outputFieldName = outputField.Name;
                    return true;
                }
            }

            return false;
        }

        private static bool AreAssignedValuesEqual(object left, object right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            if (left is System.Collections.IEnumerable leftEnumerable && right is System.Collections.IEnumerable rightEnumerable && left is not string && right is not string)
            {
                IEnumerator leftEnumerator = leftEnumerable.GetEnumerator();
                IEnumerator rightEnumerator = rightEnumerable.GetEnumerator();
                while (true)
                {
                    bool hasLeft = leftEnumerator.MoveNext();
                    bool hasRight = rightEnumerator.MoveNext();
                    if (hasLeft != hasRight)
                    {
                        return false;
                    }

                    if (!hasLeft)
                    {
                        return true;
                    }

                    if (!Equals(leftEnumerator.Current, rightEnumerator.Current))
                    {
                        return false;
                    }
                }
            }

            return Equals(left, right);
        }

        private bool TryGetOutputBinding(LinkPoint outputPoint, out MonoBehaviour outputService, out FieldInfo outputField, out Type outputFieldType, out bool expectsList)
        {
            outputService = GetService(outputPoint != null ? outputPoint.NodeId : null);
            outputField = null;
            outputFieldType = null;
            expectsList = false;
            if (outputService == null || string.IsNullOrEmpty(outputPoint.FieldName))
            {
                return false;
            }

            outputField = GetField(outputService.GetType(), outputPoint.FieldName);
            if (outputField == null)
            {
                return false;
            }

            outputFieldType = outputField.FieldType;
            expectsList = outputFieldType != null && outputFieldType.IsGenericType && outputFieldType.GetGenericTypeDefinition() == typeof(List<>);
            return outputFieldType != null;
        }

        private bool TryGetInputBinding(LinkPoint inputPoint, out Service inputService, out FieldInfo inputField, out Type expectedType, out bool expectsList, out bool isVarInput)
        {
            inputService = GetService(inputPoint != null ? inputPoint.NodeId : null);
            inputField = null;
            expectedType = null;
            expectsList = false;
            isVarInput = false;
            if (inputService == null || string.IsNullOrEmpty(inputPoint.FieldName))
            {
                return false;
            }

            inputField = GetField(inputService.GetType(), inputPoint.FieldName);
            if (inputField == null)
            {
                return false;
            }

            isVarInput = IsVarField(inputField.FieldType);
            if (isVarInput)
            {
                expectedType = inputPoint.ValueType;
                expectsList = inputPoint.ExpectsList;
                return expectedType != null;
            }

            expectedType = inputField.FieldType;
            expectsList = expectedType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(expectedType);
            return expectedType != null;
        }

        private static bool IsVarField(Type fieldType)
        {
            return fieldType != null && (typeof(MMVar).IsAssignableFrom(fieldType) || typeof(MMListVar).IsAssignableFrom(fieldType));
        }

        private static FieldInfo GetField(Type type, string fieldName)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            return null;
        }

        private static bool BindInputVarToService(Service inputService, FieldInfo inputField, bool expectsList, MonoBehaviour outputService)
        {
            if (inputService == null || inputField == null || outputService == null)
            {
                return false;
            }

            object inputVarObject = inputField.GetValue(inputService);
            if (inputVarObject == null)
            {
                inputVarObject = System.Activator.CreateInstance(inputField.FieldType);
            }

            if (inputVarObject is MMVar singleVar)
            {
                UnityEditor.Undo.RecordObject(inputService, "Bind Input Var Output");
                singleVar.type = InputType.Output;
                singleVar.service = outputService;
                inputField.SetValue(inputService, inputVarObject);
                UnityEditor.EditorUtility.SetDirty(inputService);
                return true;
            }

            if (inputVarObject is MMListVar listVar)
            {
                UnityEditor.Undo.RecordObject(inputService, "Bind Input Var Output");
                listVar.type = InputType.Output;
                listVar.service = outputService;
                inputField.SetValue(inputService, inputVarObject);
                UnityEditor.EditorUtility.SetDirty(inputService);
                return true;
            }

            return false;
        }

        private static bool ClearInputValue(Service inputService, FieldInfo inputField)
        {
            if (inputService == null || inputField == null)
            {
                return false;
            }

            object inputValue = inputField.GetValue(inputService);
            if (inputValue is MMVar singleVar)
            {
                UnityEditor.Undo.RecordObject(inputService, "Clear Input Value");
                singleVar.obj = null;
                singleVar.service = null;
                singleVar.global = string.Empty;
                singleVar.type = singleVar.SupportsDefaultInput ? InputType.Default : singleVar.GetFallbackInputType();
                inputField.SetValue(inputService, inputValue);
                UnityEditor.EditorUtility.SetDirty(inputService);
                return true;
            }

            if (inputValue is MMListVar listVar)
            {
                UnityEditor.Undo.RecordObject(inputService, "Clear Input Value");
                if (listVar.objs == null)
                {
                    listVar.objs = new List<GameObject>();
                }
                else
                {
                    listVar.objs.Clear();
                }

                listVar.service = null;
                listVar.global = string.Empty;
                listVar.type = listVar.SupportsDefaultInput ? InputType.Default : listVar.GetFallbackInputType();
                inputField.SetValue(inputService, inputValue);
                UnityEditor.EditorUtility.SetDirty(inputService);
                return true;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(inputField.FieldType))
            {
                UnityEditor.Undo.RecordObject(inputService, "Clear Input Value");
                inputField.SetValue(inputService, null);
                UnityEditor.EditorUtility.SetDirty(inputService);
                return true;
            }

            if (typeof(System.Collections.IList).IsAssignableFrom(inputField.FieldType))
            {
                UnityEditor.Undo.RecordObject(inputService, "Clear Input Value");
                object emptyList = null;
                if (!inputField.FieldType.IsInterface && !inputField.FieldType.IsAbstract)
                {
                    try
                    {
                        emptyList = Activator.CreateInstance(inputField.FieldType);
                    }
                    catch
                    {
                        emptyList = null;
                    }
                }

                inputField.SetValue(inputService, emptyList);
                UnityEditor.EditorUtility.SetDirty(inputService);
                return true;
            }

            return false;
        }

        private static void AddUnique<T>(List<T> list, T value)
        {
            if (list == null || EqualityComparer<T>.Default.Equals(value, default))
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(list[i], value))
                {
                    return;
                }
            }

            list.Add(value);
        }

        private static bool ContainsValue<T>(List<T> list, T value)
        {
            if (list == null)
            {
                return false;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(list[i], value))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RemoveValue<T>(List<T> list, T value)
        {
            if (list == null)
            {
                return;
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (EqualityComparer<T>.Default.Equals(list[i], value))
                {
                    list.RemoveAt(i);
                }
            }
        }

        private static bool AssignOutputValueToInput(Service inputService, FieldInfo inputField, Type expectedType, bool expectsList, MonoBehaviour outputService)
        {
            if (inputService == null || inputField == null || outputService == null || expectedType == null)
            {
                return false;
            }

            object value;
            if (!TryResolveAssignableOutputValue(outputService, expectedType, expectsList, out value))
            {
                return false;
            }

            UnityEditor.Undo.RecordObject(inputService, "Assign Output To Input");
            inputField.SetValue(inputService, value);
            UnityEditor.EditorUtility.SetDirty(inputService);
            return true;
        }

        private static bool TryResolveAssignableOutputValue(MonoBehaviour outputService, Type expectedType, bool expectsList, out object value)
        {
            value = null;
            if (outputService == null || expectedType == null)
            {
                return false;
            }

            if (!expectsList)
            {
                string error;
                return OutputUtility.TryGetOutputValue(outputService, expectedType, out value, out error);
            }

            FieldInfo outputField;
            string fieldError;
            if (!OutputUtility.TryGetOutputField(outputService.GetType(), out outputField, out fieldError) || outputField == null)
            {
                return false;
            }

            object rawValue = outputField.GetValue(outputService);
            if (rawValue == null)
            {
                return true;
            }

            System.Collections.IEnumerable enumerable = rawValue as System.Collections.IEnumerable;
            if (enumerable == null)
            {
                return false;
            }

            Type listType = typeof(List<>).MakeGenericType(expectedType);
            System.Collections.IList list = Activator.CreateInstance(listType) as System.Collections.IList;
            if (list == null)
            {
                return false;
            }

            foreach (object item in enumerable)
            {
                if (item == null)
                {
                    continue;
                }

                if (expectedType.IsInstanceOfType(item))
                {
                    list.Add(item);
                    continue;
                }

                if (item is GameObject go)
                {
                    if (expectedType == typeof(GameObject))
                    {
                        list.Add(go);
                        continue;
                    }

                    if (typeof(Component).IsAssignableFrom(expectedType))
                    {
                        Component component = go.GetComponent(expectedType);
                        if (component != null)
                        {
                            list.Add(component);
                        }
                    }
                }
            }

            value = list;
            return true;
        }

    }
}



