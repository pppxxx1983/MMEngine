using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SP;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public sealed class UINode : VisualElement
    {
        private const string RootClassName = "ui-node";
        private const string DefaultStateClassName = "ui-node--default";
        private const string SelectedStateClassName = "ui-node--selected";
        private const string RunningStateClassName = "ui-node--running";
        private const string CompletedStateClassName = "ui-node--completed";
        private const string StyleSheetPath = "Assets/PlayableFramework/Editor/EditorUI/Styles/UINode.uss";

        private static StyleSheet cachedStyleSheet;

        private readonly NodeData data;
        private readonly NodeLayout contentLayout;
        private readonly NodeTitle titleView;
        private EnterNextPoint enterNextPoint;
        private readonly List<ParamPoint> paramPoints = new List<ParamPoint>();
        private readonly List<OutputPoint> outputPoints = new List<OutputPoint>();
        private readonly Dictionary<string, Vector2> dragStartPositions = new Dictionary<string, Vector2>();

        private Vector2 lastPosition;
        private Vector2 startPosition;
        private bool hasMoved;
        private bool isPointerDown;
        private bool isMirrorUpdating;
        private bool hasMirrorSizeLock;

        public NodeData Data => data;
        public LinkPoint EnterPoint => enterNextPoint != null ? enterNextPoint.EnterPoint : null;
        public LinkPoint NextPoint => enterNextPoint != null ? enterNextPoint.NextPoint : null;

        public LinkPoint GetInputPoint(string fieldName)
        {
            for (int i = 0; i < paramPoints.Count; i++)
            {
                ParamPoint paramPoint = paramPoints[i];
                if (paramPoint != null && paramPoint.FieldName == fieldName)
                {
                    return paramPoint;
                }
            }

            return null;
        }

        public LinkPoint GetOutputPoint(string fieldName)
        {
            for (int i = 0; i < outputPoints.Count; i++)
            {
                OutputPoint outputPoint = outputPoints[i];
                if (outputPoint != null && outputPoint.FieldName == fieldName)
                {
                    return outputPoint.Point;
                }
            }

            return null;
        }

        public UINode(NodeData data)
        {
            this.data = data;
            focusable = true;
            AddToClassList(RootClassName);
            style.position = Position.Absolute;

            StyleSheet styleSheet = GetStyleSheet();
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }

            contentLayout = new NodeLayout();
            Add(contentLayout);

            titleView = new NodeTitle();
            titleView.MirrorClicked = OnMirrorClicked;
            contentLayout.Add(titleView);

            AddPoints();

            RegisterEvents();
            Refresh();
        }

        public void Refresh()
        {
            titleView.SetTitle(Data.Title);
            titleView.SetMirrorVisible(ServiceRule.Instance.HasMirror(Data.Id));
            ApplyMirror();
            style.left = Data.Position.x;
            style.top = Data.Position.y;
            if (!hasMirrorSizeLock)
            {
                style.width = StyleKeyword.Auto;
                style.height = StyleKeyword.Auto;
                style.minWidth = StyleKeyword.Auto;
                style.minHeight = StyleKeyword.Auto;
            }
            ApplyBorderStateClass();
        }

        public void RefreshBindings()
        {
            for (int i = 0; i < paramPoints.Count; i++)
            {
                ParamPoint paramPoint = paramPoints[i];
                if (paramPoint != null)
                {
                    paramPoint.RefreshBinding();
                }
            }
        }

        public string GetBindingSignature()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < paramPoints.Count; i++)
            {
                ParamPoint paramPoint = paramPoints[i];
                if (paramPoint == null)
                {
                    continue;
                }

                builder.Append(paramPoint.GetBindingSignature());
                builder.Append('#');
            }

            return builder.ToString();
        }

        private void RegisterEvents()
        {
            RegisterCallback<PointerDownEvent>(OnNodePointerDown);
            RegisterCallback<PointerMoveEvent>(OnNodePointerMove);
            RegisterCallback<PointerUpEvent>(OnNodePointerUp);
            RegisterCallback<PointerCaptureOutEvent>(OnNodePointerCaptureOut);
            RegisterCallback<GeometryChangedEvent>(OnNodeGeometryChanged);
        }

        private void AddPoints()
        {
            Service service = ServiceRule.Instance.GetService(Data.Id);
            ServiceRule.Instance.GetFlowPorts(Data.Id, out bool hasEnterPort, out bool hasNextPort);

            if (hasEnterPort || hasNextPort)
            {
                enterNextPoint = new EnterNextPoint();
                enterNextPoint.SetPortVisible(hasEnterPort, hasNextPort);
                contentLayout.Add(enterNextPoint);
            }

            List<FieldInfo> inputFields = ServiceRule.Instance.GetInputFields(Data.Id);
            for (int i = 0; i < inputFields.Count; i++)
            {
                ParamPoint paramPoint = new ParamPoint();
                paramPoint.Setup(service, inputFields[i]);
                paramPoints.Add(paramPoint);
                contentLayout.Add(paramPoint);
            }

            List<FieldInfo> outputFields = ServiceRule.Instance.GetOutputFields(Data.Id);
            for (int i = 0; i < outputFields.Count; i++)
            {
                OutputPoint outputPoint = new OutputPoint();
                outputPoint.Setup(service, outputFields[i]);
                outputPoints.Add(outputPoint);
                contentLayout.Add(outputPoint);
            }

            ApplyMirror();
        }

        private void OnMouseDown(Vector2 pointerPosition, bool additiveSelection)
        {
            if (!Data.IsSelected || additiveSelection)
            {
                NodeManager.Instance.SelectNode(this, additiveSelection);
            }

            lastPosition = pointerPosition;
            startPosition = Data.Position;
            dragStartPositions.Clear();

            List<UINode> selectedNodes = NodeManager.Instance.GetSelectedUINodes();
            for (int i = 0; i < selectedNodes.Count; i++)
            {
                UINode selectedNode = selectedNodes[i];
                if (selectedNode != null && selectedNode.Data != null && !string.IsNullOrEmpty(selectedNode.Data.Id))
                {
                    dragStartPositions[selectedNode.Data.Id] = selectedNode.Data.Position;
                }
            }

            if (dragStartPositions.Count == 0 && !string.IsNullOrEmpty(Data.Id))
            {
                dragStartPositions[Data.Id] = Data.Position;
            }

            hasMoved = false;
            isPointerDown = true;
            Refresh();
        }

        private void OnMouseMove(Vector2 pointerPosition)
        {
            if (!isPointerDown)
            {
                return;
            }

            Vector2 delta = pointerPosition - lastPosition;
            if (delta == Vector2.zero)
            {
                return;
            }

            List<UINode> selectedNodes = NodeManager.Instance.GetSelectedUINodes();
            for (int i = 0; i < selectedNodes.Count; i++)
            {
                UINode selectedNode = selectedNodes[i];
                if (selectedNode == null || selectedNode.Data == null || string.IsNullOrEmpty(selectedNode.Data.Id))
                {
                    continue;
                }

                Vector2 selectedStartPosition;
                if (dragStartPositions.TryGetValue(selectedNode.Data.Id, out selectedStartPosition))
                {
                    selectedNode.Data.Position = selectedStartPosition + delta;
                }
            }

            if (!Data.IsSelected)
            {
                Data.Position = startPosition + delta;
            }

            hasMoved = true;
            RefreshNodeViews();
        }

        private void OnMouseUp(Vector2 pointerPosition)
        {
            isPointerDown = false;
            if (hasMoved)
            {
                NodeManager.Instance.NotifyPosChanged();
            }
        }

        private void OnNodePointerDown(PointerDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
            {
                return;
            }

            Focus();
            this.CapturePointer(evt.pointerId);
            bool additiveSelection = evt.ctrlKey || evt.commandKey;
            OnMouseDown(new Vector2(evt.position.x, evt.position.y), additiveSelection);
            evt.StopPropagation();
        }

        private void OnNodePointerMove(PointerMoveEvent evt)
        {
            if (!isPointerDown || !this.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            OnMouseMove(new Vector2(evt.position.x, evt.position.y));
            evt.StopPropagation();
        }

        private void OnNodePointerUp(PointerUpEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse || !this.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            OnMouseUp(new Vector2(evt.position.x, evt.position.y));
            this.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnNodePointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (isPointerDown && hasMoved)
            {
                NodeManager.Instance.NotifyPosChanged();
            }

            isPointerDown = false;
        }

        private void OnNodeGeometryChanged(GeometryChangedEvent evt)
        {
            if (isMirrorUpdating)
            {
                return;
            }

            RefreshLineLayers();
        }

        private void FinishMirrorUpdate()
        {
            isMirrorUpdating = false;
            hasMirrorSizeLock = false;
            style.width = StyleKeyword.Auto;
            style.height = StyleKeyword.Auto;
            style.minWidth = StyleKeyword.Auto;
            style.minHeight = StyleKeyword.Auto;
            RefreshLineLayers();
        }

        private void RefreshLineLayers()
        {
            Curve curve = UIManager.Instance.Curve;
            if (curve != null)
            {
                curve.MarkDirtyRepaint();
            }

            VarLine varLine = UIManager.Instance.VarLine;
            if (varLine != null)
            {
                varLine.MarkDirtyRepaint();
            }
        }

        private void OnMirrorClicked()
        {
            if (!ServiceRule.Instance.HasMirror(Data.Id))
            {
                return;
            }

            hasMirrorSizeLock = true;
            style.width = resolvedStyle.width;
            style.height = resolvedStyle.height;
            style.minWidth = resolvedStyle.width;
            style.minHeight = resolvedStyle.height;
            isMirrorUpdating = true;
            EditorUIWindow.SuppressHierarchySyncOnce();
            ServiceRule.Instance.ToggleMirror(Data.Id);
            Refresh();
            schedule.Execute(FinishMirrorUpdate);
        }

        private static StyleSheet GetStyleSheet()
        {
            if (cachedStyleSheet == null)
            {
                cachedStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);
            }

            return cachedStyleSheet;
        }

        private void ApplyBorderStateClass()
        {
            NodeBorderState borderState = Data.BorderState;

            if (Data.IsSelected)
            {
                borderState = NodeBorderState.Selected;
            }

            RemoveFromClassList(DefaultStateClassName);
            RemoveFromClassList(SelectedStateClassName);
            RemoveFromClassList(RunningStateClassName);
            RemoveFromClassList(CompletedStateClassName);

            if (borderState == NodeBorderState.Selected)
            {
                AddToClassList(SelectedStateClassName);
            }
            else if (borderState == NodeBorderState.Running)
            {
                AddToClassList(RunningStateClassName);
            }
            else if (borderState == NodeBorderState.Completed)
            {
                AddToClassList(CompletedStateClassName);
            }
            else
            {
                AddToClassList(DefaultStateClassName);
            }
        }

        private void RefreshNodeViews()
        {
            VisualElement canvas = parent;
            if (canvas == null)
            {
                Refresh();
                return;
            }

            int childCount = canvas.childCount;
            for (int i = 0; i < childCount; i++)
            {
                UINode node = canvas[i] as UINode;
                if (node != null)
                {
                    node.Refresh();
                }
            }

            Curve curve = UIManager.Instance.Curve;
            if (curve != null)
            {
                curve.MarkDirtyRepaint();
            }
        }

        private void ApplyMirror()
        {
            bool isMirror = ServiceRule.Instance.GetMirror(Data.Id);
            if (enterNextPoint != null)
            {
                enterNextPoint.SetMirror(isMirror);
            }

            for (int i = 0; i < paramPoints.Count; i++)
            {
                ParamPoint paramPoint = paramPoints[i];
                if (paramPoint != null)
                {
                    paramPoint.SetMirror(isMirror);
                }
            }

            for (int i = 0; i < outputPoints.Count; i++)
            {
                OutputPoint outputPoint = outputPoints[i];
                if (outputPoint != null)
                {
                    outputPoint.SetMirror(isMirror);
                }
            }
        }
    }
}
