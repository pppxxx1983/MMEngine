using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public class HLayout : VisualElement
    {
        public bool ShowDebugBorder
        {
            get => resolvedStyle.borderLeftWidth > 0f;
            set
            {
                if (value)
                {
                    ControlDebugHelper.Apply(this, DebugFrameType.Layout);
                }
                else
                {
                    ControlDebugHelper.Clear(this);
                }
            }
        }

        public HLayout()
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;
            style.marginTop = 0f;
            style.marginBottom = 0f;
            style.alignSelf = Align.Stretch;
            style.flexGrow = 1f;
            style.flexShrink = 0f;
            style.minWidth = 0f;
            ShowDebugBorder = true;
        }
    }
}
