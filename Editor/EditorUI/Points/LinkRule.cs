using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    internal static class LinkRule
    {
        public static bool TryConnect(LinkPoint startPoint, LinkPoint endPoint)
        {
            if (!CanConnect(startPoint, endPoint))
            {
                return false;
            }

            if (IsValueLink(startPoint, endPoint))
            {
                LinkPoint outputPoint = startPoint.Type == LinkPointType.Output ? startPoint : endPoint;
                LinkPoint inputPoint = outputPoint == startPoint ? endPoint : startPoint;
                return ServiceRule.Instance.TryApplyValue(outputPoint, inputPoint);
            }

            LinkPoint nextPoint;
            LinkPoint enterPoint;
            GetOrderedPoints(startPoint, endPoint, out nextPoint, out enterPoint);

            UINode nextNode = GetOwnerNode(nextPoint);
            UINode enterNode = GetOwnerNode(enterPoint);
            if (nextNode == null || enterNode == null || nextNode.Data == null || enterNode.Data == null)
            {
                return false;
            }

            string nextNodeId = nextNode.Data.Id;
            string enterNodeId = enterNode.Data.Id;
            if (string.IsNullOrEmpty(nextNodeId) || string.IsNullOrEmpty(enterNodeId))
            {
                return false;
            }

            GameObject nextNodeObject;
            GameObject enterNodeObject;
            if (!GameObjectOperator.TryGetNodeObject(nextNodeId, out nextNodeObject) ||
                !GameObjectOperator.TryGetNodeObject(enterNodeId, out enterNodeObject))
            {
                return false;
            }

            if (nextNodeObject == null || enterNodeObject == null || nextNodeObject == enterNodeObject)
            {
                return false;
            }

            Undo.SetTransformParent(enterNodeObject.transform, nextNodeObject.transform, "Link Editor UI Node");
            return true;
        }

        public static bool CanConnect(LinkPoint startPoint, LinkPoint endPoint)
        {
            if (startPoint == null || endPoint == null || startPoint == endPoint)
            {
                return false;
            }

            bool isNextToEnter = startPoint.Type == LinkPointType.Next && endPoint.Type == LinkPointType.Enter;
            bool isEnterToNext = startPoint.Type == LinkPointType.Enter && endPoint.Type == LinkPointType.Next;
            bool isOutputToInput = startPoint.Type == LinkPointType.Output && endPoint.Type == LinkPointType.Input;
            bool isInputToOutput = startPoint.Type == LinkPointType.Input && endPoint.Type == LinkPointType.Output;
            if (!isNextToEnter && !isEnterToNext && !isOutputToInput && !isInputToOutput)
            {
                return false;
            }

            UINode startNode = GetOwnerNode(startPoint);
            UINode endNode = GetOwnerNode(endPoint);
            if (startNode == null || endNode == null || startNode == endNode)
            {
                return false;
            }

            if (startNode.Data == null || endNode.Data == null)
            {
                return false;
            }

            if (isOutputToInput || isInputToOutput)
            {
                LinkPoint outputPoint = startPoint.Type == LinkPointType.Output ? startPoint : endPoint;
                LinkPoint inputPoint = outputPoint == startPoint ? endPoint : startPoint;
                return ServiceRule.Instance.CanConnectValue(outputPoint, inputPoint);
            }

            return !string.IsNullOrEmpty(startNode.Data.Id) && !string.IsNullOrEmpty(endNode.Data.Id);
        }

        public static UINode GetOwnerNode(LinkPoint linkPoint)
        {
            VisualElement element = linkPoint;
            while (element != null)
            {
                UINode node = element as UINode;
                if (node != null)
                {
                    return node;
                }

                element = element.parent;
            }

            return null;
        }

        private static void GetOrderedPoints(LinkPoint startPoint, LinkPoint endPoint, out LinkPoint nextPoint, out LinkPoint enterPoint)
        {
            nextPoint = null;
            enterPoint = null;

            if (startPoint.Type == LinkPointType.Next && endPoint.Type == LinkPointType.Enter)
            {
                nextPoint = startPoint;
                enterPoint = endPoint;
            }
            else if (startPoint.Type == LinkPointType.Enter && endPoint.Type == LinkPointType.Next)
            {
                nextPoint = endPoint;
                enterPoint = startPoint;
            }
        }

        private static bool IsValueLink(LinkPoint startPoint, LinkPoint endPoint)
        {
            return (startPoint.Type == LinkPointType.Output && endPoint.Type == LinkPointType.Input) ||
                   (startPoint.Type == LinkPointType.Input && endPoint.Type == LinkPointType.Output);
        }
    }
}
