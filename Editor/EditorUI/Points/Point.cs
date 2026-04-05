using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public sealed class Point : VisualElement
    {
        private static readonly Color OuterRingColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        private static readonly Color InnerFillColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        private static readonly Color DotColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        private Color dotColor = DotColor;

        public Point()
        {
            pickingMode = PickingMode.Position;
            style.width = 16f;
            style.height = 16f;
            style.minWidth = 16f;
            style.minHeight = 16f;
            style.flexShrink = 0f;
            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            Rect rect = contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            Vector2 center = rect.center;
            float outerRadius = Mathf.Max(2f, Mathf.Min(rect.width, rect.height) * 0.5f);
            float innerRadius = Mathf.Max(1f, outerRadius - 2f);
            float dotRadius = Mathf.Max(1.5f, outerRadius * 0.35f);

            Painter2D painter = context.painter2D;

            painter.fillColor = OuterRingColor;
            painter.BeginPath();
            painter.Arc(center, outerRadius, 0f, 360f);
            painter.Fill();

            painter.fillColor = InnerFillColor;
            painter.BeginPath();
            painter.Arc(center, innerRadius, 0f, 360f);
            painter.Fill();

            painter.fillColor = dotColor;
            painter.BeginPath();
            painter.Arc(center, dotRadius, 0f, 360f);
            painter.Fill();
        }

        public void SetColor(Color color)
        {
            dotColor = color;
            MarkDirtyRepaint();
        }
    }
}
