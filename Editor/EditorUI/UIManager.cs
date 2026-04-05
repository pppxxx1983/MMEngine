using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace PlayableFramework.Editor
{
    public sealed class UIManager
    {
        private static readonly Color VarLineColor = Color.white;

        private static UIManager instance;

        private VisualElement root;
        private bool isLineMode;
        private LinkPoint startLinkPoint;

        public static UIManager Instance => instance ??= new UIManager();

        public EditorUIWindow CurrentWindow { get; private set; }
        public VisualElement Root => root;
        public VisualElement Canvas { get; private set; }
        public Curve Curve { get; private set; }
        public VarLine VarLine { get; private set; }
        public Line Line { get; private set; }
        public SelectionBox SelectionBox { get; private set; }

        private UIManager()
        {
            EnsureWindow();
        }

        public EditorUIWindow EnsureWindow()
        {
            if (CurrentWindow == null)
            {
                CurrentWindow = EditorWindow.GetWindow<EditorUIWindow>("Playable Editor UI");
                CurrentWindow.minSize = new Vector2(720f, 480f);
            }

            return CurrentWindow;
        }

        public void SetRoot(VisualElement rootElement)
        {
            if (root == rootElement)
            {
                return;
            }

            VisualElement oldRoot = root;
            root = rootElement;

            if (oldRoot != null)
            {
                oldRoot.UnregisterCallback<MouseMoveEvent>(OnGlobalMouseMove, TrickleDown.TrickleDown);
                oldRoot.UnregisterCallback<MouseUpEvent>(OnGlobalMouseUp, TrickleDown.TrickleDown);
            }

            if (root != null)
            {
                root.RegisterCallback<MouseMoveEvent>(OnGlobalMouseMove, TrickleDown.TrickleDown);
                root.RegisterCallback<MouseUpEvent>(OnGlobalMouseUp, TrickleDown.TrickleDown);
            }
        }

        public void SetCanvas(VisualElement canvasElement)
        {
            Canvas = canvasElement;
        }

        public void SetCurve(Curve curveElement)
        {
            Curve = curveElement;
        }

        public void SetVarLine(VarLine varLineElement)
        {
            VarLine = varLineElement;
        }

        public void SetLine(Line lineElement)
        {
            Line = lineElement;
        }

        public void SetSelectionBox(SelectionBox selectionBoxElement)
        {
            SelectionBox = selectionBoxElement;
        }

        public void BeginLine(LinkPoint linkPoint, Vector2 worldPosition)
        {
            if (Canvas == null || Line == null || linkPoint == null)
            {
                return;
            }

            NodeManager.Instance.ClearSelection();
            if (Curve != null)
            {
                Curve.ClearSelection();
            }

            UpdateLinkPointStates(linkPoint);
            Vector2 localPosition = Canvas.WorldToLocal(worldPosition);
            startLinkPoint = linkPoint;
            isLineMode = true;
            Line.Start = localPosition;
            Line.End = localPosition + new Vector2(1f, 1f);
            Line.StrokeColor = IsVarPoint(linkPoint) ? VarLineColor : new Color(0.25f, 0.55f, 0.95f, 1f);
            Line.IsVisible = true;
            Line.MarkDirtyRepaint();
        }

        private void OnGlobalMouseMove(MouseMoveEvent evt)
        {
            if (!isLineMode || Canvas == null || Line == null)
            {
                return;
            }

            Line.End = Canvas.WorldToLocal(evt.mousePosition);
            Line.MarkDirtyRepaint();
        }

        private void OnGlobalMouseUp(MouseUpEvent evt)
        {
            if (!isLineMode || Canvas == null || Line == null)
            {
                return;
            }

            LinkPoint endLinkPoint = NodeManager.Instance.GetCurrentMouseLinkPoint();
            LinkRule.TryConnect(startLinkPoint, endLinkPoint);

            Line.End = Canvas.WorldToLocal(evt.mousePosition);
            isLineMode = false;
            startLinkPoint = null;
            ResetLinkPointStates();
            VarLine?.MarkDirtyRepaint();
            Curve?.MarkDirtyRepaint();
            Line.IsVisible = false;
            Line.MarkDirtyRepaint();
        }

        private static bool IsVarPoint(LinkPoint linkPoint)
        {
            if (linkPoint == null)
            {
                return false;
            }

            return linkPoint.Type == LinkPointType.Input || linkPoint.Type == LinkPointType.Output;
        }

        private void UpdateLinkPointStates(LinkPoint startPoint)
        {
            List<LinkPoint> points = GetAllLinkPoints();
            for (int i = 0; i < points.Count; i++)
            {
                LinkPoint point = points[i];
                if (point == null)
                {
                    continue;
                }

                if (point == startPoint)
                {
                    point.SetState(LinkPointState.Valid);
                    continue;
                }

                point.SetState(LinkRule.CanConnect(startPoint, point) ? LinkPointState.Valid : LinkPointState.Invalid);
            }
        }

        private void ResetLinkPointStates()
        {
            List<LinkPoint> points = GetAllLinkPoints();
            for (int i = 0; i < points.Count; i++)
            {
                LinkPoint point = points[i];
                if (point != null)
                {
                    point.SetState(LinkPointState.Default);
                }
            }
        }

        private List<LinkPoint> GetAllLinkPoints()
        {
            List<LinkPoint> points = new List<LinkPoint>();
            if (Root == null)
            {
                return points;
            }

            CollectLinkPoints(Root, points);
            return points;
        }

        private static void CollectLinkPoints(VisualElement element, List<LinkPoint> points)
        {
            if (element is LinkPoint linkPoint)
            {
                points.Add(linkPoint);
            }

            int childCount = element.childCount;
            for (int i = 0; i < childCount; i++)
            {
                CollectLinkPoints(element[i], points);
            }
        }
    }
}
