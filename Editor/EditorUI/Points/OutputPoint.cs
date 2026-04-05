using System;
using System.Collections.Generic;
using System.Reflection;
using SP;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public sealed class OutputPoint : HLayout
    {
        private readonly Label typeLabel;
        private readonly LinkPoint point;

        public OutputPoint()
        {
            style.alignSelf = Align.Stretch;
            style.flexGrow = 1f;
            style.flexShrink = 1f;
            style.justifyContent = Justify.FlexEnd;
            style.marginBottom = 2f;

            typeLabel = new Label();
            typeLabel.style.flexGrow = 1f;
            typeLabel.style.flexShrink = 1f;
            typeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            typeLabel.style.marginRight = 6f;
            Add(typeLabel);

            point = new LinkPoint(LinkPointType.Output);
            point.SetReverseOrder(false);
            point.style.flexGrow = 0f;
            point.style.flexShrink = 0f;
            Add(point);
        }

        public void SetType(Type fieldType)
        {
            typeLabel.text = GetTypeName(fieldType);
        }

        public void Setup(Service service, FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
            {
                return;
            }

            SetType(fieldInfo.FieldType);
            point.FieldName = fieldInfo.Name;
            point.NodeId = service != null ? SceneNodeFactory.GetSceneNodeId(service.gameObject) : null;
            if (fieldInfo.FieldType != null && fieldInfo.FieldType.IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type[] arguments = fieldInfo.FieldType.GetGenericArguments();
                point.ValueType = arguments != null && arguments.Length == 1 ? arguments[0] : fieldInfo.FieldType;
                point.ExpectsList = true;
                return;
            }

            point.ValueType = fieldInfo.FieldType;
            point.ExpectsList = false;
        }

        public LinkPoint Point => point;
        public string FieldName => point.FieldName;

        public void SetMirror(bool mirror)
        {
            if (mirror)
            {
                style.flexDirection = FlexDirection.RowReverse;
                style.justifyContent = Justify.FlexStart;
                typeLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                typeLabel.style.marginLeft = 6f;
                typeLabel.style.marginRight = 0f;
                point.SetReverseOrder(true);
                return;
            }

            style.flexDirection = FlexDirection.Row;
            style.justifyContent = Justify.FlexEnd;
            typeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            typeLabel.style.marginLeft = 0f;
            typeLabel.style.marginRight = 6f;
            point.SetReverseOrder(false);
        }

        private static string GetTypeName(Type fieldType)
        {
            if (fieldType == null)
            {
                return "Output";
            }

            if (fieldType.IsGenericType)
            {
                Type genericType = fieldType.GetGenericTypeDefinition();
                if (genericType == typeof(List<>))
                {
                    Type[] arguments = fieldType.GetGenericArguments();
                    if (arguments != null && arguments.Length == 1)
                    {
                        return "List<" + GetTypeName(arguments[0]) + ">";
                    }
                }
            }

            if (fieldType == typeof(float))
            {
                return "Float";
            }

            if (fieldType == typeof(int))
            {
                return "Int";
            }

            return fieldType.Name;
        }
    }
}
