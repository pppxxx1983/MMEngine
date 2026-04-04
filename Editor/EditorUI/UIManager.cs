using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public sealed class UIManager
    {
        private static UIManager instance;

        private VisualElement root;
        private bool isLineMode;

        public static UIManager Instance => instance ??= new UIManager();

        public EditorUIWindow CurrentWindow { get; private set; }
        public VisualElement Root => root;
        public VisualElement Canvas { get; private set; }
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

        public void SetLine(Line lineElement)
        {
            Line = lineElement;
        }

        public void SetSelectionBox(SelectionBox selectionBoxElement)
        {
            SelectionBox = selectionBoxElement;
        }

        public void BeginLine(Vector2 worldPosition)
        {
            if (Canvas == null || Line == null)
            {
                return;
            }

            Vector2 localPosition = Canvas.WorldToLocal(worldPosition);
            isLineMode = true;
            Line.Start = localPosition;
            Line.End = localPosition + new Vector2(1f, 1f);
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

            Line.End = Canvas.WorldToLocal(evt.mousePosition);
            isLineMode = false;
            Line.IsVisible = false;
            Line.MarkDirtyRepaint();
        }
    }
}
