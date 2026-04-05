using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public sealed class MirrorBtn : VisualElement
    {
        private static readonly Color BorderColor = new Color(0.35f, 0.4f, 0.48f, 1f);
        private static readonly Color FillColor = new Color(0.17f, 0.19f, 0.23f, 1f);
        private static readonly Color ArrowColor = new Color(0.86f, 0.9f, 0.96f, 1f);

        public MirrorBtn()
        {
            pickingMode = PickingMode.Position;
            style.width = 18f;
            style.height = 18f;
            style.minWidth = 18f;
            style.minHeight = 18f;
            style.flexShrink = 0f;
            style.marginLeft = 6f;
            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            Rect rect = contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            Painter2D painter = context.painter2D;
            painter.fillColor = FillColor;
            painter.strokeColor = BorderColor;
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMax));
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.ClosePath();
            painter.Fill();
            painter.Stroke();

            DrawArrow(painter, rect.center + new Vector2(-1f, -3f), -1f);
            DrawArrow(painter, rect.center + new Vector2(1f, 3f), 1f);
        }

        private static void DrawArrow(Painter2D painter, Vector2 center, float direction)
        {
            float body = 5f;
            float wing = 2.5f;

            painter.strokeColor = ArrowColor;
            painter.lineWidth = 1.4f;
            painter.BeginPath();
            painter.MoveTo(center + new Vector2(-body * direction, 0f));
            painter.LineTo(center + new Vector2(body * direction, 0f));
            painter.LineTo(center + new Vector2((body - wing) * direction, -wing));
            painter.MoveTo(center + new Vector2(body * direction, 0f));
            painter.LineTo(center + new Vector2((body - wing) * direction, wing));
            painter.Stroke();
        }
    }
}
