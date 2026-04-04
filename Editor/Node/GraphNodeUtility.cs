using System;
using System.Collections.Generic;
using System.Reflection;
using SP;
using UnityEditor;
using UnityEngine;

namespace PlayableFramework.Editor
{
    internal static class GraphNodeUtility
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static string FindLegacySingleConnection(List<ConnectionPoint> points, ConnectionPointType preferredType, bool isInputSide)
        {
            ConnectionPoint typed = FindLegacyPointByType(points, preferredType);
            if (typed != null && !string.IsNullOrEmpty(typed.SingleConnectedNodeId))
            {
                return typed.SingleConnectedNodeId;
            }

            ConnectionPoint bySide = FindLegacyPointBySide(points, isInputSide);
            return bySide != null ? bySide.SingleConnectedNodeId : null;
        }

        public static List<string> FindLegacyConnections(List<ConnectionPoint> points, ConnectionPointType preferredType, bool isInputSide)
        {
            List<string> result = new List<string>();
            ConnectionPoint typed = FindLegacyPointByType(points, preferredType);
            if (typed != null)
            {
                AddConnections(result, typed.ConnectedNodeIds);
            }

            if (result.Count > 0)
            {
                return result;
            }

            ConnectionPoint bySide = FindLegacyPointBySide(points, isInputSide);
            if (bySide != null)
            {
                AddConnections(result, bySide.ConnectedNodeIds);
            }

            return result;
        }

        public static List<string> FindLegacyConnectionsByType(List<ConnectionPoint> points, ConnectionPointType type)
        {
            List<string> result = new List<string>();
            ConnectionPoint typed = FindLegacyPointByType(points, type);
            if (typed != null)
            {
                AddConnections(result, typed.ConnectedNodeIds);
            }

            return result;
        }

        public static void AddConnections(ConnectionPoint point, List<string> nodeIds)
        {
            if (point == null || nodeIds == null)
            {
                return;
            }

            for (int i = 0; i < nodeIds.Count; i++)
            {
                point.AddConnection(nodeIds[i]);
            }
        }

        public static FieldInfo FindTaggedField(Type serviceType, Type attributeType)
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

        public static List<FieldInfo> FindTaggedFields(Type serviceType, Type attributeType)
        {
            List<FieldInfo> result = new List<FieldInfo>();
            if (serviceType == null || attributeType == null)
            {
                return result;
            }

            FieldInfo[] fields = serviceType.GetFields(FieldFlags);
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i] != null && fields[i].IsDefined(attributeType, true))
                {
                    result.Add(fields[i]);
                }
            }

            result.Sort((a, b) => a.MetadataToken.CompareTo(b.MetadataToken));
            return result;
        }

        public static string GetTypeDisplayName(Type type)
        {
            if (type == null)
            {
                return "Unknown";
            }

            if (!type.IsGenericType)
            {
                return type.Name;
            }

            string typeName = type.Name;
            int tickIndex = typeName.IndexOf('`');
            if (tickIndex > 0)
            {
                typeName = typeName.Substring(0, tickIndex);
            }

            Type[] arguments = type.GetGenericArguments();
            if (arguments == null || arguments.Length == 0)
            {
                return typeName;
            }

            List<string> argNames = new List<string>(arguments.Length);
            for (int i = 0; i < arguments.Length; i++)
            {
                argNames.Add(GetTypeDisplayName(arguments[i]));
            }

            return typeName + "<" + string.Join(", ", argNames.ToArray()) + ">";
        }

        public static string GetInputTypeLabel(FieldInfo inputField)
        {
            if (inputField == null)
            {
                return "Unknown";
            }

            Type valueType;
            bool isList;
            if (TryGetVarValueType(inputField.FieldType, out valueType, out isList) && valueType != null)
            {
                return isList ? "List<" + valueType.Name + ">" : valueType.Name;
            }

            return GetTypeDisplayName(inputField.FieldType);
        }

        public static bool TryGetVarValueType(Type varFieldType, out Type valueType, out bool isList)
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

        public static void DrawRightAlignedName(Rect slotRect, GUIContent content)
        {
            string text = content != null ? content.text : string.Empty;

            GUIStyle valueStyle = new GUIStyle(EditorStyles.label);
            valueStyle.alignment = TextAnchor.MiddleRight;
            valueStyle.clipping = TextClipping.Clip;
            valueStyle.fontSize = 10;
            Rect textRect = new Rect(slotRect.x + 2f, slotRect.y, slotRect.width - 4f, slotRect.height);
            GUI.Label(textRect, text, valueStyle);
        }

        private static ConnectionPoint FindLegacyPointByType(List<ConnectionPoint> points, ConnectionPointType type)
        {
            for (int i = 0; i < points.Count; i++)
            {
                ConnectionPoint point = points[i];
                if (point != null && point.PointType == type)
                {
                    return point;
                }
            }

            return null;
        }

        private static ConnectionPoint FindLegacyPointBySide(List<ConnectionPoint> points, bool isInputSide)
        {
            for (int i = 0; i < points.Count; i++)
            {
                ConnectionPoint point = points[i];
                if (point != null && point.IsInputSide == isInputSide)
                {
                    return point;
                }
            }

            return null;
        }

        private static void AddConnections(List<string> output, IReadOnlyList<string> source)
        {
            if (source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                string nodeId = source[i];
                if (!string.IsNullOrEmpty(nodeId) && !output.Contains(nodeId))
                {
                    output.Add(nodeId);
                }
            }
        }
    }
}
