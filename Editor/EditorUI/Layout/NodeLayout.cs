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
            style.justifyContent = Justify.Center;
            ShowDebugBorder = true;
        }
    }
}
