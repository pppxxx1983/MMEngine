using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public sealed class Line : VisualElement
    {
        private static readonly Color DefaultColor = new Color(0.25f, 0.55f, 0.95f, 1f);
        private static readonly Color SelectedColor = new Color(1f, 0.82f, 0.28f, 1f);

        public bool IsVisible { get; set; }
        public Vector2 Start { get; set; }
        public Vector2 End { get; set; }
        public float Width { get; set; } = 3f;
        public bool IsSelected { get; set; }

        public Line()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0f;
            style.top = 0f;
            style.right = 0f;
            style.bottom = 0f;
            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (!IsVisible)
            {
                return;
            }

            Painter2D painter = context.painter2D;
            painter.lineWidth = Width;
            painter.strokeColor = IsSelected ? SelectedColor : DefaultColor;
            painter.BeginPath();
            painter.MoveTo(Start);
            painter.LineTo(End);
            painter.Stroke();
        }
    }
}
