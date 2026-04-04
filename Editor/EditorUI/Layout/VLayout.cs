using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public class VLayout : VisualElement
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

        public VLayout()
        {
            style.flexDirection = FlexDirection.Column;
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;
            style.alignSelf = Align.Stretch;
            style.flexGrow = 0f;
            style.flexShrink = 0f;
            style.minWidth = 0f;
            ShowDebugBorder = true;
        }
    }
}
