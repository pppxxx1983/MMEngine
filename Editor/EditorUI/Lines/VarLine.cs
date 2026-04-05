using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public sealed class VarLine : VisualElement
    {
        public readonly struct SelectedBinding
        {
            public SelectedBinding(string inputNodeId, string inputFieldName)
            {
                InputNodeId = inputNodeId;
                InputFieldName = inputFieldName;
            }

            public string InputNodeId { get; }
            public string InputFieldName { get; }
        }

        private static readonly Color DefaultColor = Color.white;
        private static readonly Color SelectedColor = new Color(1f, 0.82f, 0.28f, 0.95f);
        private const float Width = 2f;
        private const float SelectedWidth = 3f;
        private const float TangentOffset = 36f;
        private const float HitDistance = 8f;
        private const float StraightDistance = 56f;
        private const int SampleSteps = 24;

        private readonly HashSet<string> selectedVarLines = new HashSet<string>();

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

        public bool TrySelectAt(Vector2 localPosition)
        {
            VarLineHit hit;
            if (!TryGetHit(localPosition, out hit))
            {
                return false;
            }

            selectedVarLines.Clear();
            selectedVarLines.Add(GetVarLineKey(hit.OutputNodeId, hit.OutputFieldName, hit.InputNodeId, hit.InputFieldName));
            MarkDirtyRepaint();
            return true;
        }

        public void ClearSelection()
        {
            if (selectedVarLines.Count == 0)
            {
                return;
            }

            selectedVarLines.Clear();
            MarkDirtyRepaint();
        }

        public bool HasSelection()
        {
            return selectedVarLines.Count > 0;
        }

        public List<SelectedBinding> GetSelectedBindings()
        {
            List<SelectedBinding> bindings = new List<SelectedBinding>();
            foreach (string key in selectedVarLines)
            {
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                int arrowIndex = key.IndexOf("->");
                if (arrowIndex < 0 || arrowIndex + 2 >= key.Length)
                {
                    continue;
                }

                string inputPart = key.Substring(arrowIndex + 2);
                int separatorIndex = inputPart.IndexOf(':');
                if (separatorIndex <= 0 || separatorIndex + 1 >= inputPart.Length)
                {
                    continue;
                }

                bindings.Add(new SelectedBinding(
                    inputPart.Substring(0, separatorIndex),
                    inputPart.Substring(separatorIndex + 1)));
            }

            return bindings;
        }

        public void SelectInRect(Rect rect)
        {
            selectedVarLines.Clear();

            VisualElement canvas = UIManager.Instance.Canvas;
            if (canvas == null)
            {
                MarkDirtyRepaint();
                return;
            }

            int nodeCount = NodeManager.Instance.UINodes.Count;
            for (int i = 0; i < nodeCount; i++)
            {
                UINode inputNode = NodeManager.Instance.UINodes[i];
                if (inputNode == null || inputNode.Data == null)
                {
                    continue;
                }

                List<FieldInfo> inputFields = ServiceRule.Instance.GetInputFields(inputNode.Data.Id);
                for (int j = 0; j < inputFields.Count; j++)
                {
                    FieldInfo inputField = inputFields[j];
                    if (inputField == null)
                    {
                        continue;
                    }

                    VarLineHit hit;
                    if (!TryBuildHit(inputNode, inputField, canvas, out hit))
                    {
                        continue;
                    }

                    if (!IntersectsRect(rect, hit))
                    {
                        continue;
                    }

                    selectedVarLines.Add(GetVarLineKey(hit.OutputNodeId, hit.OutputFieldName, hit.InputNodeId, hit.InputFieldName));
                }
            }

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

        private void DrawNodeInputLines(UINode inputNode, VisualElement canvas, Painter2D painter)
        {
            List<FieldInfo> inputFields = ServiceRule.Instance.GetInputFields(inputNode.Data.Id);
            for (int i = 0; i < inputFields.Count; i++)
            {
                FieldInfo inputField = inputFields[i];
                if (inputField == null)
                {
                    continue;
                }

                VarLineHit hit;
                if (!TryBuildHit(inputNode, inputField, canvas, out hit))
                {
                    continue;
                }

                bool isSelected = IsSelected(hit.OutputNodeId, hit.OutputFieldName, hit.InputNodeId, hit.InputFieldName);
                painter.strokeColor = isSelected ? SelectedColor : DefaultColor;
                painter.lineWidth = isSelected ? SelectedWidth : Width;

                painter.BeginPath();
                painter.MoveTo(hit.Start);
                if (hit.IsStraight)
                {
                    painter.LineTo(hit.End);
                }
                else
                {
                    painter.BezierCurveTo(hit.StartTangent, hit.EndTangent, hit.End);
                }
                painter.Stroke();
            }
        }

        private bool TryGetHit(Vector2 localPosition, out VarLineHit bestHit)
        {
            VisualElement canvas = UIManager.Instance.Canvas;
            if (canvas == null)
            {
                bestHit = default(VarLineHit);
                return false;
            }

            float bestDistance = float.MaxValue;
            bestHit = default(VarLineHit);

            int nodeCount = NodeManager.Instance.UINodes.Count;
            for (int i = 0; i < nodeCount; i++)
            {
                UINode inputNode = NodeManager.Instance.UINodes[i];
                if (inputNode == null || inputNode.Data == null)
                {
                    continue;
                }

                List<FieldInfo> inputFields = ServiceRule.Instance.GetInputFields(inputNode.Data.Id);
                for (int j = 0; j < inputFields.Count; j++)
                {
                    FieldInfo inputField = inputFields[j];
                    if (inputField == null)
                    {
                        continue;
                    }

                    VarLineHit hit;
                    if (!TryBuildHit(inputNode, inputField, canvas, out hit))
                    {
                        continue;
                    }

                    float distance = GetBezierDistance(localPosition, hit);
                    if (distance > HitDistance || distance >= bestDistance)
                    {
                        continue;
                    }

                    bestDistance = distance;
                    bestHit = hit;
                }
            }

            return bestDistance < float.MaxValue;
        }

        private static bool TryBuildHit(UINode inputNode, FieldInfo inputField, VisualElement canvas, out VarLineHit hit)
        {
            hit = default(VarLineHit);
            if (inputNode == null || inputNode.Data == null || inputField == null || canvas == null)
            {
                return false;
            }

            LinkPoint inputPoint = inputNode.GetInputPoint(inputField.Name);
            if (inputPoint == null)
            {
                return false;
            }

            string outputNodeId;
            string outputFieldName;
            if (!ServiceRule.Instance.TryGetBoundOutput(inputPoint, out outputNodeId, out outputFieldName))
            {
                return false;
            }

            UINode outputNode = NodeManager.Instance.GetUINode(outputNodeId);
            if (outputNode == null)
            {
                return false;
            }

            LinkPoint outputPoint = outputNode.GetOutputPoint(outputFieldName);
            if (outputPoint == null)
            {
                return false;
            }

            Vector2 outputWorld = outputPoint.GetPointWorldPosition();
            Vector2 inputWorld = inputPoint.GetPointWorldPosition();
            Vector2 start = canvas.WorldToLocal(outputWorld);
            Vector2 end = canvas.WorldToLocal(inputWorld);
            float startDirection = outputWorld.x <= outputNode.worldBound.center.x ? -1f : 1f;
            float endDirection = inputWorld.x <= inputNode.worldBound.center.x ? -1f : 1f;
            float tangent = GetTangentOffset(start, end);

            hit = new VarLineHit
            {
                OutputNodeId = outputNodeId,
                OutputFieldName = outputFieldName,
                InputNodeId = inputNode.Data.Id,
                InputFieldName = inputField.Name,
                Start = start,
                End = end,
                IsStraight = Vector2.Distance(start, end) <= StraightDistance,
                StartTangent = start + new Vector2(tangent * startDirection, 0f),
                EndTangent = end + new Vector2(tangent * endDirection, 0f)
            };
            return true;
        }

        private bool IsSelected(string outputNodeId, string outputFieldName, string inputNodeId, string inputFieldName)
        {
            if (selectedVarLines.Contains(GetVarLineKey(outputNodeId, outputFieldName, inputNodeId, inputFieldName)))
            {
                return true;
            }

            UINode outputNode = NodeManager.Instance.GetUINode(outputNodeId);
            if (outputNode != null && outputNode.Data != null && outputNode.Data.IsSelected)
            {
                return true;
            }

            UINode inputNode = NodeManager.Instance.GetUINode(inputNodeId);
            return inputNode != null && inputNode.Data != null && inputNode.Data.IsSelected;
        }

        private static float GetTangentOffset(Vector2 start, Vector2 end)
        {
            float horizontalDistance = Mathf.Abs(end.x - start.x);
            if (end.x < start.x)
            {
                return Mathf.Clamp(horizontalDistance * 0.25f, 16f, 24f);
            }

            return Mathf.Clamp(horizontalDistance * 0.4f, 16f, TangentOffset);
        }

        private static string GetVarLineKey(string outputNodeId, string outputFieldName, string inputNodeId, string inputFieldName)
        {
            return outputNodeId + ":" + outputFieldName + "->" + inputNodeId + ":" + inputFieldName;
        }

        private static bool IntersectsRect(Rect rect, VarLineHit hit)
        {
            if (hit.IsStraight)
            {
                return SegmentIntersectsRect(hit.Start, hit.End, rect);
            }

            Vector2 previous = hit.Start;
            if (rect.Contains(previous))
            {
                return true;
            }

            for (int i = 1; i <= SampleSteps; i++)
            {
                float t = i / (float)SampleSteps;
                Vector2 current = EvaluateBezier(hit.Start, hit.StartTangent, hit.EndTangent, hit.End, t);
                if (rect.Contains(current) || SegmentIntersectsRect(previous, current, rect))
                {
                    return true;
                }

                previous = current;
            }

            return false;
        }

        private static float GetBezierDistance(Vector2 point, VarLineHit hit)
        {
            if (hit.IsStraight)
            {
                return DistanceToSegment(point, hit.Start, hit.End);
            }

            Vector2 previous = hit.Start;
            float bestDistance = float.MaxValue;

            for (int i = 1; i <= SampleSteps; i++)
            {
                float t = i / (float)SampleSteps;
                Vector2 current = EvaluateBezier(hit.Start, hit.StartTangent, hit.EndTangent, hit.End, t);
                float distance = DistanceToSegment(point, previous, current);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                }

                previous = current;
            }

            return bestDistance;
        }

        private static Vector2 EvaluateBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;
            return uuu * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + ttt * p3;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSq = ab.sqrMagnitude;
            if (lengthSq <= 0f)
            {
                return Vector2.Distance(point, a);
            }

            float t = Vector2.Dot(point - a, ab) / lengthSq;
            t = Mathf.Clamp01(t);
            Vector2 projection = a + ab * t;
            return Vector2.Distance(point, projection);
        }

        private static bool SegmentIntersectsRect(Vector2 a, Vector2 b, Rect rect)
        {
            if (rect.Contains(a) || rect.Contains(b))
            {
                return true;
            }

            Vector2 topLeft = new Vector2(rect.xMin, rect.yMin);
            Vector2 topRight = new Vector2(rect.xMax, rect.yMin);
            Vector2 bottomLeft = new Vector2(rect.xMin, rect.yMax);
            Vector2 bottomRight = new Vector2(rect.xMax, rect.yMax);

            return SegmentsIntersect(a, b, topLeft, topRight) ||
                   SegmentsIntersect(a, b, topRight, bottomRight) ||
                   SegmentsIntersect(a, b, bottomRight, bottomLeft) ||
                   SegmentsIntersect(a, b, bottomLeft, topLeft);
        }

        private static bool SegmentsIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
        {
            float d1 = Cross(a2 - a1, b1 - a1);
            float d2 = Cross(a2 - a1, b2 - a1);
            float d3 = Cross(b2 - b1, a1 - b1);
            float d4 = Cross(b2 - b1, a2 - b1);

            bool hasDifferentA = (d1 > 0f && d2 < 0f) || (d1 < 0f && d2 > 0f);
            bool hasDifferentB = (d3 > 0f && d4 < 0f) || (d3 < 0f && d4 > 0f);
            return hasDifferentA && hasDifferentB;
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        private struct VarLineHit
        {
            public string OutputNodeId;
            public string OutputFieldName;
            public string InputNodeId;
            public string InputFieldName;
            public Vector2 Start;
            public Vector2 End;
            public bool IsStraight;
            public Vector2 StartTangent;
            public Vector2 EndTangent;
        }
    }
}
