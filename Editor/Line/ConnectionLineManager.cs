using System;
using System.Collections.Generic;
using System.Reflection;
using SP;
using SP.SceneRefs;
using UnityEditor;
using UnityEngine;

namespace PlayableFramework.Editor
{
    internal sealed class ConnectionLineManager
    {
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Color ParentConnectionColor = new Color(0.25f, 0.55f, 0.95f, 1f);
        private static readonly Color DataConnectionColor = new Color(0.95f, 0.82f, 0.28f, 1f);
        private const float NormalLineWidth = 3f;
        private const float SelectedLineWidth = 6f;

        public ConnectionLineManager()
        {
        }

        public void Draw(Vector2 canvasOffset)
        {
            GraphManager manager = GraphManager.Instance;
            if (manager == null || manager.Nodes == null)
            {
                return;
            }

            Handles.BeginGUI();
            DrawParentConnections(canvasOffset);
            DrawDataConnections(canvasOffset);
            Handles.EndGUI();
        }

        private void DrawParentConnections(Vector2 canvasOffset)
        {
            var manager= GraphManager.Instance;
            for (int i = 0; i < manager.Nodes.Count; i++)
            {
                GraphNode childNode = manager.Nodes[i];
                if (childNode == null)
                {
                    continue;
                }

                ConnectionPoint childEnter = childNode.EnterPoint;
                if (childEnter == null || string.IsNullOrEmpty(childEnter.SingleConnectedNodeId))
                {
                    continue;
                }

                GraphNode parentNode = manager.GetNodeById(childEnter.SingleConnectedNodeId);
                if (parentNode == null)
                {
                    continue;
                }

                ConnectionPoint parentNext = parentNode.NextPoint;
                if (parentNext == null)
                {
                    continue;
                }

                Vector2 start = parentNext.GetCanvasCenter(canvasOffset);
                Vector2 end = childEnter.GetCanvasCenter(canvasOffset);
                bool isSelected = manager.IsSelectedParentLine(parentNode.Id, childNode.Id);
                Handles.color = ParentConnectionColor;
                DrawBezier(start, end, isSelected ? SelectedLineWidth : NormalLineWidth);
            }
        }

        private void DrawDataConnections(Vector2 canvasOffset)
        {
            var manager=GraphManager.Instance;
            for (int i = 0; i < manager.Nodes.Count; i++)
            {
                GraphNode inputNode = manager.Nodes[i];
                if (inputNode == null)
                {
                    continue;
                }

                IReadOnlyList<ConnectionPoint> inputPoints = inputNode.DataInputPoints;
                for (int j = 0; j < inputPoints.Count; j++)
                {
                    ConnectionPoint inputPoint = inputPoints[j];
                    if (inputPoint == null)
                    {
                        continue;
                    }

                    MonoBehaviour boundService;
                    if (!TryGetInputBoundService(inputNode, inputPoint, out boundService) || boundService == null)
                    {
                        continue;
                    }

                    SceneRefObject sourceRef = boundService.GetComponent<SceneRefObject>();
                    if (sourceRef == null || string.IsNullOrEmpty(sourceRef.Id))
                    {
                        continue;
                    }

                    GraphNode outputNode = manager.GetNodeById(sourceRef.Id);
                    if (outputNode == null)
                    {
                        continue;
                    }

                    ConnectionPoint outputPoint = outputNode.DataOutputPoint;
                    if (outputPoint == null)
                    {
                        continue;
                    }

                    Vector2 start = outputPoint.GetCanvasCenter(canvasOffset);
                    Vector2 end = inputPoint.GetCanvasCenter(canvasOffset);
                    bool isSelected = manager.IsSelectedDataLine(outputNode.Id, inputNode.Id, j);
                    Handles.color = DataConnectionColor;
                    DrawBezier(start, end, isSelected ? SelectedLineWidth : NormalLineWidth);
                }
            }
        }

        private bool TryGetInputBoundService(GraphNode node, ConnectionPoint inputPoint, out MonoBehaviour boundService)
        {
            boundService = null;
            if (node == null || inputPoint == null)
            {
                return false;
            }

            GameObject nodeObject;
            var manager = GraphManager.Instance;
            if (!manager.TryGetNodeObject(node, out nodeObject) || nodeObject == null)
            {
                return false;
            }

            Service nodeService = GetNodeService(nodeObject);
            if (nodeService == null)
            {
                return false;
            }

            List<FieldInfo> inputFields = FindTaggedFields(nodeService.GetType(), typeof(InputAttribute));
            int inputIndex = node.GetInputPointIndex(inputPoint);
            if (inputIndex < 0 || inputIndex >= inputFields.Count)
            {
                return false;
            }

            FieldInfo inputField = inputFields[inputIndex];
            object inputVar = inputField.GetValue(nodeService);
            MMVar singleVar = inputVar as MMVar;
            if (singleVar != null)
            {
                if (singleVar.type == InputType.Output && singleVar.service != null)
                {
                    boundService = singleVar.service;
                    return true;
                }

                return false;
            }

            MMListVar listVar = inputVar as MMListVar;
            if (listVar != null)
            {
                if (listVar.type == InputType.Output && listVar.service != null)
                {
                    boundService = listVar.service;
                    return true;
                }

                return false;
            }

            return false;
        }

        private static Service GetNodeService(GameObject nodeObject)
        {
            if (nodeObject == null)
            {
                return null;
            }

            Service[] services = nodeObject.GetComponents<Service>();
            if (services == null || services.Length == 0)
            {
                return null;
            }

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

        private static List<FieldInfo> FindTaggedFields(Type serviceType, Type attributeType)
        {
            List<FieldInfo> result = new List<FieldInfo>();
            if (serviceType == null || attributeType == null)
            {
                return result;
            }

            FieldInfo[] fields = serviceType.GetFields(FieldFlags);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (field != null && field.IsDefined(attributeType, true))
                {
                    result.Add(field);
                }
            }

            result.Sort((a, b) => a.MetadataToken.CompareTo(b.MetadataToken));
            return result;
        }

        private static void DrawBezier(Vector2 start, Vector2 end, float width)
        {
            float tangentOffset = Mathf.Max(60f, Mathf.Abs(end.x - start.x) * 0.5f);
            Vector2 startTangent = start + Vector2.right * tangentOffset;
            Vector2 endTangent = end + Vector2.left * tangentOffset;
            Handles.DrawBezier(start, end, startTangent, endTangent, Handles.color, null, width);
        }
    }
}

