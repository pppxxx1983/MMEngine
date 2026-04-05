using System;
using System.Collections.Generic;
using System.Reflection;
using SP;
using UnityEditor;
using UnityEngine;

namespace PlayableFramework.Editor
{
    internal sealed class GraphNodeGameObjectLogic
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public Type ResolveServiceType(GameObject nodeObject, Type fallbackType)
        {
            if (nodeObject == null)
            {
                return fallbackType;
            }

            Service[] services = nodeObject.GetComponents<Service>();
            for (int i = 0; i < services.Length; i++)
            {
                Service service = services[i];
                if (service != null && service.GetType() != typeof(Service))
                {
                    return service.GetType();
                }
            }

            for (int i = 0; i < services.Length; i++)
            {
                if (services[i] != null)
                {
                    return services[i].GetType();
                }
            }

            return fallbackType;
        }

        public void ResolveRootPortFlags(GameObject nodeObject, Type serviceType, out bool hasEnter, out bool hasNext)
        {
            hasEnter = true;
            hasNext = true;

            Service serviceInstance = ResolveServiceInstance(nodeObject);
            if (serviceInstance != null)
            {
                IFlowPort runtimeConfig = serviceInstance as IFlowPort;
                if (runtimeConfig != null)
                {
                    hasEnter = runtimeConfig.HasEnterPort;
                    hasNext = runtimeConfig.HasNextPort;
                }

                return;
            }

            if (serviceType == null || !typeof(IFlowPort).IsAssignableFrom(serviceType) || serviceType.IsAbstract)
            {
                return;
            }

            try
            {
                object created = Activator.CreateInstance(serviceType);
                IFlowPort config = created as IFlowPort;
                if (config != null)
                {
                    hasEnter = config.HasEnterPort;
                    hasNext = config.HasNextPort;
                }
            }
            catch
            {
                hasEnter = true;
                hasNext = true;
            }
        }

        public string GetOutputFieldName(GraphNode node, Type fallbackServiceType)
        {
            if (node == null)
            {
                return string.Empty;
            }

            GameObject nodeObject;
            GraphManager manager = GraphManager.Instance;
            if (manager != null && manager.TryGetNodeObject(node, out nodeObject) && nodeObject != null)
            {
                Service service = ResolveServiceInstance(nodeObject);
                if (service != null)
                {
                    FieldInfo outputField = FindTaggedField(service.GetType(), typeof(OutputAttribute));
                    if (outputField != null)
                    {
                        return outputField.Name ?? string.Empty;
                    }
                }
            }

            FieldInfo fallback = FindTaggedField(fallbackServiceType, typeof(OutputAttribute));
            return fallback != null ? (fallback.Name ?? string.Empty) : string.Empty;
        }

        public bool TryGetInputPointInputType(
            GraphNode node,
            ConnectionPoint inputPoint,
            GraphManager manager,
            IList<string> inputFieldNames,
            out InputType inputType)
        {
            inputType = InputType.Default;
            if (node == null || inputFieldNames == null)
            {
                return false;
            }

            int inputIndex = node.GetInputPointIndex(inputPoint);
            if (inputIndex < 0 || inputIndex >= inputFieldNames.Count)
            {
                return false;
            }

            GameObject nodeObject;
            if (manager == null || !manager.TryGetNodeObject(node, out nodeObject) || nodeObject == null)
            {
                return false;
            }

            Service service = ResolveServiceInstance(nodeObject);
            if (service == null)
            {
                return false;
            }

            FieldInfo field = GetFieldFromTypeHierarchy(service.GetType(), inputFieldNames[inputIndex]);
            if (field == null)
            {
                return false;
            }

            object raw = field.GetValue(service);
            MMVar singleVar = raw as MMVar;
            if (singleVar != null)
            {
                inputType = singleVar.GetResolvedInputType();
                return true;
            }

            MMListVar listVar = raw as MMListVar;
            if (listVar != null)
            {
                inputType = listVar.GetResolvedInputType();
                return true;
            }

            return false;
        }

        public GUIContent BuildInputValueContent(GraphNode node, ConnectionPoint inputPoint, IList<string> inputFieldNames)
        {
            GraphManager manager = GraphManager.Instance;
            if (node == null || inputFieldNames == null)
            {
                return new GUIContent("None");
            }

            int inputIndex = node.GetInputPointIndex(inputPoint);
            if (inputIndex >= 0 && inputIndex < inputFieldNames.Count)
            {
                GameObject nodeObject;
                if (manager != null && manager.TryGetNodeObject(node, out nodeObject) && nodeObject != null)
                {
                    Service service = ResolveServiceInstance(nodeObject);
                    if (service != null)
                    {
                        FieldInfo field = GetFieldFromTypeHierarchy(service.GetType(), inputFieldNames[inputIndex]);
                        if (field != null)
                        {
                            Type valueType;
                            bool isListType;
                            TryGetVarValueType(field.FieldType, out valueType, out isListType);
                            object raw = field.GetValue(service);
                            MMVar singleVar = raw as MMVar;
                            if (singleVar != null)
                            {
                                return BuildSingleVarContent(singleVar, valueType);
                            }

                            MMListVar listVar = raw as MMListVar;
                            if (listVar != null)
                            {
                                return BuildListVarContent(listVar, valueType);
                            }
                        }
                    }
                }
            }

            return new GUIContent("None");
        }

        public GUIContent BuildOutputValueContent(GraphNode node, ConnectionPoint outputPoint)
        {
            GameObject nodeObject;
            GraphManager manager = GraphManager.Instance;
            if (node == null || manager == null || !manager.TryGetNodeObject(node, out nodeObject) || nodeObject == null)
            {
                return new GUIContent("None");
            }

            Service service = ResolveServiceInstance(nodeObject);
            if (service == null)
            {
                return new GUIContent("None");
            }

            FieldInfo outputField = FindTaggedField(service.GetType(), typeof(OutputAttribute));
            if (outputField == null)
            {
                return new GUIContent("None");
            }

            object outputValue = outputField.GetValue(service);
            return BuildOutputGameObjectContent(outputValue);
        }

        public Service ResolveServiceInstance(GameObject nodeObject)
        {
            if (nodeObject == null)
            {
                return null;
            }

            Service[] services = nodeObject.GetComponents<Service>();
            for (int i = 0; i < services.Length; i++)
            {
                Service service = services[i];
                if (service != null && service.GetType() != typeof(Service))
                {
                    return service;
                }
            }

            for (int i = 0; i < services.Length; i++)
            {
                if (services[i] != null)
                {
                    return services[i];
                }
            }

            return null;
        }

        private static FieldInfo FindTaggedField(Type serviceType, Type attributeType)
        {
            if (serviceType == null || attributeType == null)
            {
                return null;
            }

            FieldInfo[] fields = serviceType.GetFields(FieldFlags);
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i] != null && fields[i].IsDefined(attributeType, true))
                {
                    return fields[i];
                }
            }

            return null;
        }

        private static GUIContent BuildSingleVarContent(MMVar var, Type valueType)
        {
            if (var == null)
            {
                return new GUIContent("None");
            }

            InputType inputType = var.GetResolvedInputType();
            if (inputType == InputType.Output && var.service != null)
            {
                object serviceValue;
                if (TryResolveServiceValue(var.service, valueType, false, out serviceValue))
                {
                    return BuildObjectNameContent(serviceValue);
                }

                return BuildObjectNameContent(var.service.gameObject);
            }

            if (inputType == InputType.Global && !string.IsNullOrEmpty(var.global))
            {
                object globalValue;
                if (TryResolveGlobalValue(var.global, valueType, false, out globalValue))
                {
                    return BuildObjectNameContent(globalValue);
                }

                return new GUIContent("None");
            }

            if (var.obj == null)
            {
                return new GUIContent("None");
            }

            return BuildObjectNameContent(var.obj);
        }

        private static GUIContent BuildListVarContent(MMListVar listVar, Type valueType)
        {
            if (listVar == null)
            {
                return new GUIContent("None");
            }

            InputType inputType = listVar.GetResolvedInputType();
            if (inputType == InputType.Output && listVar.service != null)
            {
                object serviceValue;
                if (TryResolveServiceValue(listVar.service, valueType, true, out serviceValue))
                {
                    return BuildObjectNameContent(serviceValue);
                }
            }

            if (inputType == InputType.Global && !string.IsNullOrEmpty(listVar.global))
            {
                object globalValue;
                if (TryResolveGlobalValue(listVar.global, valueType, true, out globalValue))
                {
                    return BuildObjectNameContent(globalValue);
                }
            }

            if (listVar.objs == null || listVar.objs.Count == 0 || listVar.objs[0] == null)
            {
                return new GUIContent("None");
            }

            return BuildObjectNameContent(listVar.objs[0]);
        }

        private static GUIContent BuildObjectNameContent(object value)
        {
            if (value == null)
            {
                return new GUIContent("None");
            }

            GameObject go = value as GameObject;
            if (go != null)
            {
                return new GUIContent(go.name);
            }

            Component component = value as Component;
            if (component != null)
            {
                return new GUIContent(component.gameObject != null ? component.gameObject.name : component.name);
            }

            UnityEngine.Object unityObject = value as UnityEngine.Object;
            if (unityObject != null)
            {
                return new GUIContent(unityObject.name);
            }

            return new GUIContent(FormatValueForDisplay(value));
        }

        private static bool TryResolveServiceValue(MonoBehaviour sourceService, Type valueType, bool expectsList, out object resolved)
        {
            resolved = null;
            if (sourceService == null)
            {
                return false;
            }

            FieldInfo outputField;
            string error;
            if (!OutputUtility.TryGetOutputField(sourceService.GetType(), out outputField, out error) || outputField == null)
            {
                return false;
            }

            object raw = outputField.GetValue(sourceService);
            if (expectsList)
            {
                return TryResolveFirstFromEnumerable(raw, valueType, out resolved);
            }

            return TryConvertObjectForDisplay(raw, valueType, out resolved);
        }

        private static bool TryResolveGlobalValue(string globalKey, Type valueType, bool expectsList, out object resolved)
        {
            resolved = null;
            if (string.IsNullOrEmpty(globalKey))
            {
                return false;
            }

            GlobalContext globalInstance = FindGlobalInstance();
            if (globalInstance == null)
            {
                return false;
            }

            object raw;
            bool ok = expectsList
                ? globalInstance.TryGetListValue(globalKey, valueType != null ? valueType : typeof(GameObject), out raw)
                : globalInstance.TryGetValue(globalKey, valueType != null ? valueType : typeof(GameObject), out raw);
            if (!ok)
            {
                return false;
            }

            if (expectsList)
            {
                return TryResolveFirstFromEnumerable(raw, valueType, out resolved);
            }

            return TryConvertObjectForDisplay(raw, valueType, out resolved);
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

        private static bool TryResolveFirstFromEnumerable(object enumerableObject, Type valueType, out object resolved)
        {
            resolved = null;
            if (enumerableObject == null)
            {
                return false;
            }

            System.Collections.IEnumerable enumerable = enumerableObject as System.Collections.IEnumerable;
            if (enumerable == null)
            {
                return false;
            }

            foreach (object item in enumerable)
            {
                if (TryConvertObjectForDisplay(item, valueType, out resolved))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryConvertObjectForDisplay(object raw, Type targetType, out object converted)
        {
            converted = null;
            if (raw == null)
            {
                return false;
            }

            if (targetType == null)
            {
                converted = raw;
                return true;
            }

            GameObject go = raw as GameObject;
            if (go != null)
            {
                if (targetType == typeof(GameObject))
                {
                    converted = go;
                    return true;
                }

                if (typeof(Component).IsAssignableFrom(targetType))
                {
                    Component c = go.GetComponent(targetType);
                    if (c != null)
                    {
                        converted = c;
                        return true;
                    }
                }
            }

            Component comp = raw as Component;
            if (comp != null)
            {
                if (targetType == typeof(GameObject))
                {
                    converted = comp.gameObject;
                    return true;
                }

                if (targetType.IsAssignableFrom(comp.GetType()))
                {
                    converted = comp;
                    return true;
                }

                if (typeof(Component).IsAssignableFrom(targetType))
                {
                    Component c = comp.GetComponent(targetType);
                    if (c != null)
                    {
                        converted = c;
                        return true;
                    }
                }
            }

            if (targetType.IsInstanceOfType(raw))
            {
                converted = raw;
                return true;
            }

            return false;
        }

        private static GUIContent BuildOutputGameObjectContent(object value)
        {
            if (value == null)
            {
                return new GUIContent("None");
            }

            GameObject go = value as GameObject;
            if (go != null)
            {
                return new GUIContent(go.name);
            }

            Component component = value as Component;
            if (component != null)
            {
                GameObject owner = component.gameObject;
                return new GUIContent(owner != null ? owner.name : component.name);
            }

            return new GUIContent(FormatValueForDisplay(value));
        }

        private static string FormatValueForDisplay(object value)
        {
            if (value == null)
            {
                return "None";
            }

            if (value is Vector2 v2)
            {
                return $"({v2.x:0.###}, {v2.y:0.###})";
            }

            if (value is Vector3 v3)
            {
                return $"({v3.x:0.###}, {v3.y:0.###}, {v3.z:0.###})";
            }

            if (value is Vector4 v4)
            {
                return $"({v4.x:0.###}, {v4.y:0.###}, {v4.z:0.###}, {v4.w:0.###})";
            }

            if (value is float f)
            {
                return f.ToString("0.###");
            }

            if (value is double d)
            {
                return d.ToString("0.###");
            }

            if (value is bool b)
            {
                return b ? "True" : "False";
            }

            return value.ToString();
        }

        private static FieldInfo GetFieldFromTypeHierarchy(Type type, string fieldName)
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

        private static bool TryGetVarValueType(Type varFieldType, out Type valueType, out bool isList)
        {
            valueType = null;
            isList = false;
            if (TryResolveValueTypeFromGetMethod(varFieldType, out Type methodValueType, out bool methodIsList))
            {
                valueType = methodValueType;
                isList = methodIsList;
                return valueType != null;
            }

            Type current = varFieldType;
            while (current != null)
            {
                if (current.IsGenericType)
                {
                    Type genericTypeDefinition = current.GetGenericTypeDefinition();
                    if (genericTypeDefinition == typeof(MMVar<>))
                    {
                        Type[] arguments = current.GetGenericArguments();
                        valueType = arguments != null && arguments.Length == 1 ? arguments[0] : null;
                        return valueType != null;
                    }

                    if (genericTypeDefinition == typeof(MMListVar<>))
                    {
                        Type[] arguments = current.GetGenericArguments();
                        valueType = arguments != null && arguments.Length == 1 ? arguments[0] : null;
                        isList = true;
                        return valueType != null;
                    }
                }

                current = current.BaseType;
            }

            return false;
        }

        private static bool TryResolveValueTypeFromGetMethod(Type type, out Type valueType, out bool isListType)
        {
            valueType = null;
            isListType = false;
            if (type == null)
            {
                return false;
            }

            MethodInfo getter = type.GetMethod("Get", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
            if (getter == null)
            {
                return false;
            }

            Type returnType = getter.ReturnType;
            if (returnType == null || returnType == typeof(void))
            {
                return false;
            }

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type[] genericArguments = returnType.GetGenericArguments();
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
    }
}


