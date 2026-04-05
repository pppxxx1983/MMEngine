using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public sealed class EnterNextPoint : HLayout
    {
        private const float RowHeight = 16f;
        private const float SlotFontSize = 8f;
        private static readonly Color EnterPlateColor = new Color(0.18f, 0.42f, 0.31f, 0.95f);
        private static readonly Color NextPlateColor = new Color(0.23f, 0.36f, 0.58f, 0.95f);

        private readonly LinkPoint enterPoint;
        private readonly Label enterLabel;
        private readonly HLayout leftLayout;
        private readonly LinkPoint nextPoint;
        private readonly Label nextLabel;
        private readonly HLayout rightLayout;

        public EnterNextPoint()
        {
            style.alignSelf = Align.Stretch;
            style.justifyContent = Justify.SpaceBetween;
            style.alignItems = Align.Center;
            style.marginBottom = 2f;
            style.height = RowHeight;
            style.minHeight = RowHeight;
            style.maxHeight = RowHeight;

            leftLayout = new HLayout();
            leftLayout.style.flexGrow = 1f;
            leftLayout.style.flexShrink = 1f;
            leftLayout.style.justifyContent = Justify.FlexStart;
            leftLayout.style.alignItems = Align.Center;
            leftLayout.style.height = RowHeight;
            leftLayout.style.minHeight = RowHeight;
            leftLayout.style.maxHeight = RowHeight;
            Add(leftLayout);

            enterPoint = new LinkPoint(LinkPointType.Enter);
            enterPoint.SetReverseOrder(true);
            enterPoint.style.flexGrow = 0f;
            enterPoint.style.flexShrink = 0f;
            enterPoint.style.alignSelf = Align.Center;
            leftLayout.Add(enterPoint);

            enterLabel = new Label("Enter");
            enterLabel.style.height = RowHeight;
            enterLabel.style.minHeight = RowHeight;
            enterLabel.style.fontSize = SlotFontSize;
            enterLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            enterLabel.style.alignSelf = Align.Center;
            enterLabel.style.position = Position.Relative;
            enterLabel.style.top = -1f;
            enterLabel.style.backgroundColor = EnterPlateColor;
            enterLabel.style.borderTopLeftRadius = 3f;
            enterLabel.style.borderTopRightRadius = 3f;
            enterLabel.style.borderBottomLeftRadius = 3f;
            enterLabel.style.borderBottomRightRadius = 3f;
            enterLabel.style.paddingLeft = 6f;
            enterLabel.style.paddingRight = 6f;
            enterLabel.style.marginLeft = 4f;
            leftLayout.Add(enterLabel);

            rightLayout = new HLayout();
            rightLayout.style.flexGrow = 1f;
            rightLayout.style.flexShrink = 1f;
            rightLayout.style.justifyContent = Justify.FlexEnd;
            rightLayout.style.alignItems = Align.Center;
            rightLayout.style.height = RowHeight;
            rightLayout.style.minHeight = RowHeight;
            rightLayout.style.maxHeight = RowHeight;
            Add(rightLayout);

            nextLabel = new Label("Next");
            nextLabel.style.height = RowHeight;
            nextLabel.style.minHeight = RowHeight;
            nextLabel.style.fontSize = SlotFontSize;
            nextLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            nextLabel.style.alignSelf = Align.Center;
            nextLabel.style.position = Position.Relative;
            nextLabel.style.top = -1f;
            nextLabel.style.backgroundColor = NextPlateColor;
            nextLabel.style.borderTopLeftRadius = 3f;
            nextLabel.style.borderTopRightRadius = 3f;
            nextLabel.style.borderBottomLeftRadius = 3f;
            nextLabel.style.borderBottomRightRadius = 3f;
            nextLabel.style.paddingLeft = 6f;
            nextLabel.style.paddingRight = 6f;
            nextLabel.style.marginRight = 4f;
            rightLayout.Add(nextLabel);

            nextPoint = new LinkPoint(LinkPointType.Next);
            nextPoint.SetReverseOrder(false);
            nextPoint.style.flexGrow = 0f;
            nextPoint.style.flexShrink = 0f;
            nextPoint.style.alignSelf = Align.Center;
            rightLayout.Add(nextPoint);

            SetMirror(false);
        }

        public void SetMirror(bool mirror)
        {
            if (mirror)
            {
                style.flexDirection = FlexDirection.RowReverse;
                enterLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                nextLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                leftLayout.style.flexDirection = FlexDirection.RowReverse;
                rightLayout.style.flexDirection = FlexDirection.RowReverse;
                leftLayout.style.justifyContent = Justify.FlexStart;
                rightLayout.style.justifyContent = Justify.FlexEnd;
                enterPoint.SetReverseOrder(true);
                nextPoint.SetReverseOrder(true);
            }
            else
            {
                style.flexDirection = FlexDirection.Row;
                enterLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                nextLabel.style.unityTextAlign = TextAnchor.MiddleRight;
                leftLayout.style.flexDirection = FlexDirection.Row;
                rightLayout.style.flexDirection = FlexDirection.Row;
                leftLayout.style.justifyContent = Justify.FlexStart;
                rightLayout.style.justifyContent = Justify.FlexEnd;
                enterPoint.SetReverseOrder(true);
                nextPoint.SetReverseOrder(false);
            }
        }

        public void SetPortVisible(bool hasEnterPort, bool hasNextPort)
        {
            SetSideVisible(enterPoint, enterLabel, hasEnterPort);
            SetSideVisible(nextPoint, nextLabel, hasNextPort);
        }

        private static void SetSideVisible(LinkPoint point, Label label, bool isVisible)
        {
            Visibility visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
            point.style.visibility = visibility;
            label.style.visibility = visibility;
            point.pickingMode = isVisible ? PickingMode.Position : PickingMode.Ignore;
        }

        public LinkPoint EnterPoint => enterPoint;

        public LinkPoint NextPoint => nextPoint;
    }
}
