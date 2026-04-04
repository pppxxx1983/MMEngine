using UnityEngine;

namespace PlayableFramework.Editor
{
    /// <summary>
    /// Node 渲染统一上下文，后续节点绘制参数都从这里扩展。
    /// </summary>
    internal sealed class NodeRenderContext
    {
        public readonly Vector2 CanvasOffset;
        public readonly ConnectionPoint DraggingConnectionPoint;

        public NodeRenderContext(
            Vector2 canvasOffset,
            ConnectionPoint draggingConnectionPoint)
        {
            CanvasOffset = canvasOffset;
            DraggingConnectionPoint = draggingConnectionPoint;
        }
    }
}
