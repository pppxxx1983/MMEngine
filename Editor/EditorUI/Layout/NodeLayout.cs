using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public sealed class NodeLayout : VisualElement
    {
        public bool ShowDebugBorder
        {
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
        public NodeLayout()
        {
            style.alignSelf = Align.Stretch;
            style.flexDirection = FlexDirection.Column;
            style.alignItems = Align.Stretch;
            style.justifyContent = Justify.Center;
            style.paddingTop = 2f;
            style.paddingBottom = 4f;
            ShowDebugBorder = true;
        }
    }
}
