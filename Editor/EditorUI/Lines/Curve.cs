using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public sealed class Curve : VisualElement
    {
        public readonly struct SelectedLink
        {
            public SelectedLink(string parentId, string childId)
            {
                ParentId = parentId;
                ChildId = childId;
            }

            public string ParentId { get; }
            public string ChildId { get; }
        }

        private static readonly Color DefaultColor = new Color(0.3f, 0.75f, 1f, 0.9f);
        private static readonly Color SelectedColor = new Color(1f, 0.82f, 0.28f, 0.95f);
        private const float Width = 2f;
        private const float SelectedWidth = 3f;
        private const float TangentOffset = 60f;
        private const float HitDistance = 8f;
        private const float StraightDistance = 56f;
        private const int SampleSteps = 24;

        private readonly HashSet<string> selectedCurves = new HashSet<string>();

        public Curve()
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
            CurveHit hit;
            if (!TryGetHit(localPosition, out hit))
            {
                return false;
            }

            selectedCurves.Clear();
            selectedCurves.Add(GetCurveKey(hit.ParentId, hit.ChildId));
            MarkDirtyRepaint();
            return true;
        }

        public void ClearSelection()
        {
            if (selectedCurves.Count == 0)
            {
                return;
            }

            selectedCurves.Clear();
            MarkDirtyRepaint();
        }

        public bool HasSelection()
        {
            return selectedCurves.Count > 0;
        }

        public List<string> GetSelectedChildIds()
        {
            List<string> childIds = new List<string>();
            List<SelectedLink> links = GetSelectedLinks();
            for (int i = 0; i < links.Count; i++)
            {
                childIds.Add(links[i].ChildId);
            }

            return childIds;
        }

        public List<SelectedLink> GetSelectedLinks()
        {
            List<SelectedLink> links = new List<SelectedLink>();
            foreach (string curveKey in selectedCurves)
            {
                int separatorIndex = curveKey.IndexOf("->");
                if (separatorIndex <= 0 || separatorIndex + 2 >= curveKey.Length)
                {
                    continue;
                }

                links.Add(new SelectedLink(
                    curveKey.Substring(0, separatorIndex),
                    curveKey.Substring(separatorIndex + 2)));
            }

            return links;
        }

        public void SelectInRect(Rect rect)
        {
            selectedCurves.Clear();

            List<CurveHit> hits = BuildHits();
            for (int i = 0; i < hits.Count; i++)
            {
                CurveHit hit = hits[i];
                if (!IntersectsRect(rect, hit))
                {
                    continue;
                }

                selectedCurves.Add(GetCurveKey(hit.ParentId, hit.ChildId));
            }

            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            Painter2D painter = context.painter2D;
            List<CurveHit> hits = BuildHits();
            for (int i = 0; i < hits.Count; i++)
            {
                CurveHit hit = hits[i];
                bool isSelected = IsSelected(hit.ParentId, hit.ChildId);
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

        private bool TryGetHit(Vector2 localPosition, out CurveHit bestHit)
        {
            float bestDistance = float.MaxValue;
            bestHit = default(CurveHit);

            List<CurveHit> hits = BuildHits();
            for (int i = 0; i < hits.Count; i++)
            {
                CurveHit hit = hits[i];
                float distance = GetBezierDistance(localPosition, hit);
                if (distance > HitDistance || distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestHit = hit;
            }

            return bestDistance < float.MaxValue;
        }

        private static List<CurveHit> BuildHits()
        {
            List<CurveHit> hits = new List<CurveHit>();
            VisualElement canvas = UIManager.Instance.Canvas;
            if (canvas == null)
            {
                return hits;
            }

            int nodeCount = NodeManager.Instance.UINodes.Count;
            for (int i = 0; i < nodeCount; i++)
            {
                UINode node = NodeManager.Instance.UINodes[i];
                if (node == null || node.Data == null || string.IsNullOrEmpty(node.Data.Id))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(node.Data.ParentId))
                {
                    UINode parentNode = NodeManager.Instance.GetUINode(node.Data.ParentId);
                    if (TryBuildHit(parentNode, node, canvas, out CurveHit hierarchyHit))
                    {
                        hits.Add(hierarchyHit);
                    }
                }

                List<string> guideEnterIds;
                if (ServiceRule.Instance.TryGetGuideEnterIds(node.Data.Id, out guideEnterIds))
                {
                    for (int j = 0; j < guideEnterIds.Count; j++)
                    {
                        string guideEnterId = guideEnterIds[j];
                        UINode enterParentNode = NodeManager.Instance.GetUINode(guideEnterId);
                        if (TryBuildHit(enterParentNode, node, canvas, out CurveHit enterHit))
                        {
                            hits.Add(enterHit);
                        }
                    }
                }

                List<string> guideNextIds;
                if (ServiceRule.Instance.TryGetGuideNextIds(node.Data.Id, out guideNextIds))
                {
                    for (int j = 0; j < guideNextIds.Count; j++)
                    {
                        string guideNextId = guideNextIds[j];
                        UINode nextChildNode = NodeManager.Instance.GetUINode(guideNextId);
                        if (TryBuildHit(node, nextChildNode, canvas, out CurveHit nextHit))
                        {
                            hits.Add(nextHit);
                        }
                    }
                }
            }

            return hits;
        }

        private static bool TryBuildHit(UINode parentNode, UINode childNode, VisualElement canvas, out CurveHit hit)
        {
            hit = default(CurveHit);
            if (parentNode == null || childNode == null || parentNode.Data == null || childNode.Data == null || canvas == null)
            {
                return false;
            }

            LinkPoint parentPoint = parentNode.NextPoint;
            LinkPoint childPoint = childNode.EnterPoint;
            if (parentPoint == null || childPoint == null)
            {
                return false;
            }

            Vector2 start = canvas.WorldToLocal(parentPoint.GetPointWorldPosition());
            Vector2 end = canvas.WorldToLocal(childPoint.GetPointWorldPosition());
            float startDirection = GetHorizontalDirection(parentPoint, parentNode);
            float endDirection = GetHorizontalDirection(childPoint, childNode);
            float tangentOffset = GetTangentOffset(start, end);
            hit = new CurveHit
            {
                ParentId = parentNode.Data.Id,
                ChildId = childNode.Data.Id,
                Start = start,
                End = end,
                IsStraight = Vector2.Distance(start, end) <= StraightDistance,
                StartTangent = start + new Vector2(tangentOffset * startDirection, 0f),
                EndTangent = end + new Vector2(tangentOffset * endDirection, 0f)
            };
            return true;
        }

        private static float GetTangentOffset(Vector2 start, Vector2 end)
        {
            float horizontalDistance = Mathf.Abs(end.x - start.x);
            if (end.x < start.x)
            {
                return Mathf.Clamp(horizontalDistance * 0.25f, 16f, 32f);
            }

            return Mathf.Clamp(horizontalDistance * 0.5f, 20f, TangentOffset);
        }

        private static float GetHorizontalDirection(LinkPoint point, UINode node)
        {
            float pointX = point.worldBound.center.x;
            float nodeCenterX = node.worldBound.center.x;
            return pointX >= nodeCenterX ? 1f : -1f;
        }

        private bool IsSelected(string parentId, string childId)
        {
            if (selectedCurves.Contains(GetCurveKey(parentId, childId)))
            {
                return true;
            }

            UINode parentNode = NodeManager.Instance.GetUINode(parentId);
            if (parentNode != null && parentNode.Data != null && parentNode.Data.IsSelected)
            {
                return true;
            }

            UINode childNode = NodeManager.Instance.GetUINode(childId);
            return childNode != null && childNode.Data != null && childNode.Data.IsSelected;
        }

        private static bool IntersectsRect(Rect rect, CurveHit hit)
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

        private static string GetCurveKey(string parentId, string childId)
        {
            return parentId + "->" + childId;
        }

        private static float GetBezierDistance(Vector2 point, CurveHit hit)
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

        private struct CurveHit
        {
            public string ParentId;
            public string ChildId;
            public Vector2 Start;
            public Vector2 End;
            public bool IsStraight;
            public Vector2 StartTangent;
            public Vector2 EndTangent;
        }
    }
}
