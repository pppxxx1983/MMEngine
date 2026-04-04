using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public enum DebugFrameType
    {
        Node,
        Layout,
    }

    public static class ControlDebugHelper
    {
        public static bool Enabled { get; set; } = false;

        public static void Apply(VisualElement element, DebugFrameType frameType)
        {
            if (element == null)
            {
                return;
            }

            if (!Enabled)
            {
                return;
            }

            Color color = GetColor(frameType);
            const float width = 1f;

            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
        }

        public static void Clear(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            element.style.borderLeftWidth = 0f;
            element.style.borderRightWidth = 0f;
            element.style.borderTopWidth = 0f;
            element.style.borderBottomWidth = 0f;
        }

        private static Color GetColor(DebugFrameType frameType)
        {
            switch (frameType)
            {
                case DebugFrameType.Node:
                    return new Color(1f, 0.55f, 0.55f, 0.9f);
                default:
                    return new Color(0.55f, 1f, 0.55f, 0.75f);
            }
        }
    }
}
