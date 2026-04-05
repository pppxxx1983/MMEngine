using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public sealed class NodeTitle : HLayout
    {
        private const float RowHeight = 18f;
        private const float TitleFontSize = 9f;

        private readonly Label titleLabel;
        private readonly MirrorBtn mirrorBtn;

        public NodeTitle()
        {
            style.alignSelf = Align.Stretch;
            style.justifyContent = Justify.SpaceBetween;
            style.alignItems = Align.Center;
            style.marginBottom = 4f;
            style.height = RowHeight;
            style.minHeight = RowHeight;
            style.maxHeight = RowHeight;

            titleLabel = new Label();
            titleLabel.style.flexGrow = 1f;
            titleLabel.style.flexShrink = 1f;
            titleLabel.style.minWidth = EditorUIDefaults.NodeTitleMinWidth;
            titleLabel.style.height = RowHeight;
            titleLabel.style.minHeight = RowHeight;
            titleLabel.style.maxHeight = RowHeight;
            titleLabel.style.fontSize = TitleFontSize;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            titleLabel.style.whiteSpace = WhiteSpace.NoWrap;
            titleLabel.style.overflow = Overflow.Hidden;
            titleLabel.style.textOverflow = TextOverflow.Ellipsis;
            Add(titleLabel);

            mirrorBtn = new MirrorBtn();
            mirrorBtn.RegisterCallback<PointerDownEvent>(OnMirrorBtnPointerDown);
            mirrorBtn.RegisterCallback<PointerUpEvent>(OnMirrorBtnPointerUp);
            Add(mirrorBtn);
        }

        public System.Action MirrorClicked { get; set; }

        public void SetTitle(string title)
        {
            titleLabel.text = title;
        }

        public void SetMirrorVisible(bool isVisible)
        {
            mirrorBtn.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnMirrorBtnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
            {
                return;
            }

            mirrorBtn.CapturePointer(evt.pointerId);
            evt.StopImmediatePropagation();
        }

        private void OnMirrorBtnPointerUp(PointerUpEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
            {
                return;
            }

            if (mirrorBtn.HasPointerCapture(evt.pointerId))
            {
                mirrorBtn.ReleasePointer(evt.pointerId);
            }

            MirrorClicked?.Invoke();
            evt.StopImmediatePropagation();
        }
    }
}
