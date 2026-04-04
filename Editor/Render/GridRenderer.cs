using UnityEditor;
using UnityEngine;

namespace PlayableFramework.Editor
{
    internal static class GridRenderer
    {
        private const float GridSmallSpacing = 20f;
        private const float GridLargeSpacing = 100f;

        private static readonly Color GridSmallColor = new Color(1f, 1f, 1f, 0.18f);
        private static readonly Color GridLargeColor = new Color(1f, 1f, 1f, 0.32f);
        private static readonly Color BackgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);

        public static void DrawBackground(Rect windowRect, Vector2 canvasOffset)
        {
            EditorGUI.DrawRect(windowRect, BackgroundColor);
            DrawGrid(windowRect, GridSmallSpacing, GridSmallColor, canvasOffset);
            DrawGrid(windowRect, GridLargeSpacing, GridLargeColor, canvasOffset);
        }

        private static void DrawGrid(Rect windowRect, float spacing, Color color, Vector2 canvasOffset)
        {
            Handles.BeginGUI();
            Handles.color = color;

            Vector2 offset = new Vector2(canvasOffset.x % spacing, canvasOffset.y % spacing);
            int verticalLines = Mathf.CeilToInt(windowRect.width / spacing) + 2;
            int horizontalLines = Mathf.CeilToInt(windowRect.height / spacing) + 2;

            for (int i = 0; i < verticalLines; i++)
            {
                float x = windowRect.x + i * spacing + offset.x;
                Handles.DrawLine(new Vector3(x, windowRect.y), new Vector3(x, windowRect.yMax));
            }

            for (int i = 0; i < horizontalLines; i++)
            {
                float y = windowRect.y + i * spacing + offset.y;
                Handles.DrawLine(new Vector3(windowRect.x, y), new Vector3(windowRect.xMax, y));
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }
    }
}
