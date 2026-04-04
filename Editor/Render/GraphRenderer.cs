using UnityEditor;
using UnityEngine;

namespace PlayableFramework.Editor
{
    /// <summary>
    /// 节点图绘制类。
    /// 只负责绘制，不处理任何事件。
    /// </summary>
    internal sealed class GraphRenderer
    {
        private static readonly Color ConnectionColor = new Color(0.3f, 0.85f, 1f, 1f);
        private readonly ConnectionLineManager lineManager;

        public GraphRenderer()
        {
            lineManager = new ConnectionLineManager();
        }

        public void Render(Rect windowRect, EditorInputHandler input)
        {
            GraphManager manager = GraphManager.Instance;
            DrawBackground(windowRect, input.CanvasOffset);
            DrawNodes(input.CanvasOffset, manager != null ? manager.DraggingConnectionPoint : null);
            lineManager.Draw(input.CanvasOffset);
            DrawPreviewConnection(
                manager != null ? manager.DraggingConnectionPoint : null,
                manager != null ? manager.DraggingConnectionMousePosition : Vector2.zero,
                input.CanvasOffset);
        }

        public void Draw(Rect windowRect, EditorInputHandler input)
        {
            Render(windowRect, input);
        }

        private void DrawBackground(Rect windowRect, Vector2 canvasOffset)
        {
            GridRenderer.DrawBackground(windowRect, canvasOffset);
        }

        private void DrawNodes(Vector2 canvasOffset, ConnectionPoint draggingConnectionPoint)
        {
            GraphManager manager = GraphManager.Instance;
            if (manager == null || manager.Nodes == null)
            {
                return;
            }

            for (int i = 0; i < manager.Nodes.Count; i++)
            {
                GraphNode node = manager.Nodes[i];
                node.Render(canvasOffset, draggingConnectionPoint);
            }
        }

        private void DrawPreviewConnection(ConnectionPoint draggingConnectionPoint, Vector2 draggingConnectionMousePosition, Vector2 canvasOffset)
        {
            if (draggingConnectionPoint == null)
            {
                return;
            }

            Handles.BeginGUI();
            Handles.color = ConnectionColor;

            Vector2 start = draggingConnectionPoint.GetCanvasCenter(canvasOffset);
            Handles.DrawLine(start, draggingConnectionMousePosition);

            Handles.EndGUI();
            GUI.changed = true;
        }

    }
}
