using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public sealed class SelectionBox : ImmediateModeElement
    {
        private static readonly Color FillColor = new Color(0.3f, 0.6f, 1f, 0.12f);
        private static readonly Color BorderColor = new Color(0.45f, 0.75f, 1f, 1f);

        public bool IsVisible { get; set; }
        public Rect Rect { get; set; }

        public SelectionBox()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0f;
            style.top = 0f;
            style.right = 0f;
            style.bottom = 0f;
        }

        protected override void ImmediateRepaint()
        {
            if (!IsVisible)
            {
                return;
            }

            Handles.BeginGUI();
            EditorGUI.DrawRect(Rect, FillColor);
            Handles.color = BorderColor;
            Handles.DrawDottedLine(new Vector3(Rect.xMin, Rect.yMin), new Vector3(Rect.xMax, Rect.yMin), 4f);
            Handles.DrawDottedLine(new Vector3(Rect.xMax, Rect.yMin), new Vector3(Rect.xMax, Rect.yMax), 4f);
            Handles.DrawDottedLine(new Vector3(Rect.xMax, Rect.yMax), new Vector3(Rect.xMin, Rect.yMax), 4f);
            Handles.DrawDottedLine(new Vector3(Rect.xMin, Rect.yMax), new Vector3(Rect.xMin, Rect.yMin), 4f);
            Handles.EndGUI();
        }
    }
}
