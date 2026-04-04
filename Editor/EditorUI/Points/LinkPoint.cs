using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public enum LinkPointType
    {
        Enter,
        Next,
        Input,
        Output
    }

    public sealed class LinkPoint : HLayout
    {
        private const string ToggleClassName = "ui-link-toggle";

        private readonly VisualElement leftSlot;
        private readonly VisualElement rightSlot;
        private readonly Toggle toggle;

        public LinkPoint(LinkPointType type)
        {
            pickingMode = PickingMode.Position;
            style.justifyContent = Justify.Center;
            style.alignItems = Align.Center;
            style.flexDirection = FlexDirection.Row;

            leftSlot = new VisualElement();
            leftSlot.style.flexShrink = 0f;
            leftSlot.style.justifyContent = Justify.FlexStart;
            leftSlot.style.alignItems = Align.Center;
            leftSlot.style.flexDirection = FlexDirection.Row;

            toggle = new Toggle();
            toggle.label = string.Empty;
            toggle.AddToClassList(ToggleClassName);
            toggle.style.width = 16f;
            toggle.style.height = 16f;
            toggle.RegisterCallback<MouseDownEvent>(OnToggleMouseDown, TrickleDown.TrickleDown);

            rightSlot = new VisualElement();
            rightSlot.style.justifyContent = Justify.FlexEnd;
            rightSlot.style.alignItems = Align.Center;
            rightSlot.style.flexDirection = FlexDirection.Row;

            Add(leftSlot);
            Add(rightSlot);
            SetReverseOrder(true);
        }

        public void SetReverseOrder(bool reverse)
        {
            leftSlot.Clear();
            rightSlot.Clear();

            if (reverse)
            {
                leftSlot.Add(toggle);
            }
            else
            {
                rightSlot.Add(toggle);
            }
        }

        public Vector2 GetPointWorldPosition()
        {
            return toggle.worldBound.center;
        }

        private void OnToggleMouseDown(MouseDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
            {
                return;
            }

            UIManager.Instance.BeginLine(GetPointWorldPosition());
            evt.StopPropagation();
        }
    }
}
