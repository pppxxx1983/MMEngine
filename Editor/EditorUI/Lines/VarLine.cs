using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public sealed class VarLine : VisualElement
    {
        private static readonly Color LineColor = Color.white;
        private const float Width = 2f;
        private const float TangentOffset = 36f;

        public VarLine()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0f;
            style.top = 0f;
            style.right = 0f;
            style.bottom = 0f;
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            NodeManager.Instance.Changed += Refresh;
            NodeManager.Instance.SelectionChanged += Refresh;
            NodeManager.Instance.PosChanged += Refresh;
            MarkDirtyRepaint();
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            NodeManager.Instance.Changed -= Refresh;
            NodeManager.Instance.SelectionChanged -= Refresh;
            NodeManager.Instance.PosChanged -= Refresh;
        }

        private void Refresh()
        {
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            VisualElement canvas = UIManager.Instance.Canvas;
            if (canvas == null)
            {
                return;
            }

            Painter2D painter = context.painter2D;
            painter.strokeColor = LineColor;
            painter.lineWidth = Width;

            int nodeCount = NodeManager.Instance.UINodes.Count;
            for (int i = 0; i < nodeCount; i++)
            {
                UINode inputNode = NodeManager.Instance.UINodes[i];
                if (inputNode == null || inputNode.Data == null)
                {
                    continue;
                }

                DrawNodeInputLines(inputNode, canvas, painter);
            }
        }

        private static void DrawNodeInputLines(UINode inputNode, VisualElement canvas, Painter2D painter)
        {
            List<FieldInfo> inputFields = ServiceRule.Instance.GetInputFields(inputNode.Data.Id);
            for (int i = 0; i < inputFields.Count; i++)
            {
                FieldInfo inputField = inputFields[i];
                if (inputField == null)
                {
                    continue;
                }

                LinkPoint inputPoint = inputNode.GetInputPoint(inputField.Name);
                if (inputPoint == null)
                {
                    continue;
                }

                string outputNodeId;
                string outputFieldName;
                if (!ServiceRule.Instance.TryGetBoundOutput(inputPoint, out outputNodeId, out outputFieldName))
                {
                    continue;
                }

                UINode outputNode = NodeManager.Instance.GetUINode(outputNodeId);
                if (outputNode == null)
                {
                    continue;
                }

                LinkPoint outputPoint = outputNode.GetOutputPoint(outputFieldName);
                if (outputPoint == null)
                {
                    continue;
                }

                Vector2 outputWorld = outputPoint.GetPointWorldPosition();
                Vector2 inputWorld = inputPoint.GetPointWorldPosition();
                Vector2 start = canvas.WorldToLocal(outputWorld);
                Vector2 end = canvas.WorldToLocal(inputWorld);
                float startDirection = outputWorld.x <= outputNode.worldBound.center.x ? -1f : 1f;
                float endDirection = inputWorld.x <= inputNode.worldBound.center.x ? -1f : 1f;
                float tangent = Mathf.Clamp(Mathf.Abs(end.x - start.x) * 0.4f, 16f, TangentOffset);

                painter.BeginPath();
                painter.MoveTo(start);
                painter.BezierCurveTo(
                    start + new Vector2(tangent * startDirection, 0f),
                    end + new Vector2(tangent * endDirection, 0f),
                    end);
                painter.Stroke();
            }
        }
    }
}
