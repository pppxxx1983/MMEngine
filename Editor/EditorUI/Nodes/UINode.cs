using System.Collections.Generic;
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
        private readonly Label titleLabel;
        private readonly EnterNextPoint enterNextPoint;
        private readonly Dictionary<string, Vector2> dragStartPositions = new Dictionary<string, Vector2>();

        private Vector2 lastPosition;
        private Vector2 startPosition;
        private bool isPointerDown;

        public NodeData Data => data;

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

            titleLabel = new Label();
            titleLabel.style.alignSelf = Align.Stretch;
            titleLabel.style.minWidth = EditorUIDefaults.NodeTitleMinWidth;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            contentLayout.Add(titleLabel);

            enterNextPoint = new EnterNextPoint();
            contentLayout.Add(enterNextPoint);

            RegisterEvents();
            Refresh();
        }

        public void Refresh()
        {
            titleLabel.text = data.Title;
            style.left = data.Position.x;
            style.top = data.Position.y;
            style.width = StyleKeyword.Auto;
            style.height = StyleKeyword.Auto;
            style.minWidth = StyleKeyword.Auto;
            style.minHeight = StyleKeyword.Auto;
            ApplyBorderStateClass();
        }

        private void RegisterEvents()
        {
            RegisterCallback<PointerDownEvent>(OnNodePointerDown);
            RegisterCallback<PointerMoveEvent>(OnNodePointerMove);
            RegisterCallback<PointerUpEvent>(OnNodePointerUp);
            RegisterCallback<PointerCaptureOutEvent>(OnNodePointerCaptureOut);
        }

        private void OnMouseDown(Vector2 pointerPosition)
        {
            if (!data.IsSelected)
            {
                NodeManager.Instance.SelectNode(data);
            }

            lastPosition = pointerPosition;
            startPosition = data.Position;
            dragStartPositions.Clear();

            List<NodeData> selectedNodes = NodeManager.Instance.GetSelectedNodes();
            for (int i = 0; i < selectedNodes.Count; i++)
            {
                NodeData selectedNode = selectedNodes[i];
                if (selectedNode != null && !string.IsNullOrEmpty(selectedNode.Id))
                {
                    dragStartPositions[selectedNode.Id] = selectedNode.Position;
                }
            }

            if (dragStartPositions.Count == 0 && !string.IsNullOrEmpty(data.Id))
            {
                dragStartPositions[data.Id] = data.Position;
            }

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
            List<NodeData> selectedNodes = NodeManager.Instance.GetSelectedNodes();
            for (int i = 0; i < selectedNodes.Count; i++)
            {
                NodeData selectedNode = selectedNodes[i];
                if (selectedNode == null || string.IsNullOrEmpty(selectedNode.Id))
                {
                    continue;
                }

                Vector2 selectedStartPosition;
                if (dragStartPositions.TryGetValue(selectedNode.Id, out selectedStartPosition))
                {
                    selectedNode.Position = selectedStartPosition + delta;
                }
            }

            if (!data.IsSelected)
            {
                data.Position = startPosition + delta;
            }

            RefreshNodeViews();
        }

        private void OnMouseUp(Vector2 pointerPosition)
        {
            isPointerDown = false;
            NodeManager.Instance.NotifyPosChanged();
        }

        private void OnNodePointerDown(PointerDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
            {
                return;
            }

            Focus();
            this.CapturePointer(evt.pointerId);
            OnMouseDown(new Vector2(evt.position.x, evt.position.y));
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
            isPointerDown = false;
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
            NodeBorderState borderState = data.BorderState;

            if (data.IsSelected)
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
        }
    }
}
