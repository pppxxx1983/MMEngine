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

            // 处理 Ref 连接（RefNext→Enter 或 RefEnter→Next）
            if (IsRefLink(startPoint, endPoint))
            {
                return TryConnectRefLink(startPoint, endPoint);
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

            if (ServiceRule.Instance.TryApplyFlow(nextNodeId, enterNodeId))
            {
                return true;
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

        private static bool IsRefLink(LinkPoint startPoint, LinkPoint endPoint)
        {
            // RefNext → Enter 或 Enter → RefNext
            bool isRefNextToEnter = (startPoint.Type == LinkPointType.RefNext && endPoint.Type == LinkPointType.Enter) ||
                                    (startPoint.Type == LinkPointType.Enter && endPoint.Type == LinkPointType.RefNext);
            // RefEnter → Next 或 Next → RefEnter
            bool isRefEnterToNext = (startPoint.Type == LinkPointType.RefEnter && endPoint.Type == LinkPointType.Next) ||
                                    (startPoint.Type == LinkPointType.Next && endPoint.Type == LinkPointType.RefEnter);
            return isRefNextToEnter || isRefEnterToNext;
        }

        private static bool TryConnectRefLink(LinkPoint startPoint, LinkPoint endPoint)
        {
            // RefNext → Enter
            if ((startPoint.Type == LinkPointType.RefNext && endPoint.Type == LinkPointType.Enter) ||
                (startPoint.Type == LinkPointType.Enter && endPoint.Type == LinkPointType.RefNext))
            {
                LinkPoint refNextPoint = startPoint.Type == LinkPointType.RefNext ? startPoint : endPoint;
                LinkPoint enterPoint = refNextPoint == startPoint ? endPoint : startPoint;

                UINode refNextNode = GetOwnerNode(refNextPoint);
                UINode enterNode = GetOwnerNode(enterPoint);

                if (refNextNode == null || enterNode == null || 
                    refNextNode.Data == null || enterNode.Data == null)
                {
                    return false;
                }

                string refNextNodeId = refNextNode.Data.Id;
                string enterNodeId = enterNode.Data.Id;

                if (string.IsNullOrEmpty(refNextNodeId) || string.IsNullOrEmpty(enterNodeId))
                {
                    return false;
                }

                // 保存 EnterId 到 RefNext 所在 Service
                return ServiceRule.Instance.TryApplyRefNextToEnter(refNextNodeId, enterNodeId);
            }

            // RefEnter → Next
            if ((startPoint.Type == LinkPointType.RefEnter && endPoint.Type == LinkPointType.Next) ||
                (startPoint.Type == LinkPointType.Next && endPoint.Type == LinkPointType.RefEnter))
            {
                LinkPoint refEnterPoint = startPoint.Type == LinkPointType.RefEnter ? startPoint : endPoint;
                LinkPoint nextPoint = refEnterPoint == startPoint ? endPoint : startPoint;

                UINode refEnterNode = GetOwnerNode(refEnterPoint);
                UINode nextNode = GetOwnerNode(nextPoint);

                if (refEnterNode == null || nextNode == null || 
                    refEnterNode.Data == null || nextNode.Data == null)
                {
                    return false;
                }

                string refEnterNodeId = refEnterNode.Data.Id;
                string nextNodeId = nextNode.Data.Id;

                if (string.IsNullOrEmpty(refEnterNodeId) || string.IsNullOrEmpty(nextNodeId))
                {
                    return false;
                }

                // 保存 NextId 到 RefEnter 所在 Service 的 IRefPort.NextId
                return ServiceRule.Instance.TryApplyRefEnterToNext(refEnterNodeId, nextNodeId);
            }

            return false;
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
            bool isRefLink = IsRefLink(startPoint, endPoint);
            
            if (!isNextToEnter && !isEnterToNext && !isOutputToInput && !isInputToOutput && !isRefLink)
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
