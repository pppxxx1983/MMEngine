using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public sealed class EnterNextPoint : HLayout
    {
        private const float RowHeight = 16f;

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

            leftLayout = new HLayout();
            leftLayout.style.flexGrow = 1f;
            leftLayout.style.flexShrink = 1f;
            leftLayout.style.justifyContent = Justify.FlexStart;
            leftLayout.style.alignItems = Align.Center;
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
            enterLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            enterLabel.style.alignSelf = Align.Center;
            enterLabel.style.position = Position.Relative;
            enterLabel.style.top = -1f;
            leftLayout.Add(enterLabel);

            rightLayout = new HLayout();
            rightLayout.style.flexGrow = 1f;
            rightLayout.style.flexShrink = 1f;
            rightLayout.style.justifyContent = Justify.FlexEnd;
            rightLayout.style.alignItems = Align.Center;
            Add(rightLayout);

            nextLabel = new Label("Next");
            nextLabel.style.height = RowHeight;
            nextLabel.style.minHeight = RowHeight;
            nextLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            nextLabel.style.alignSelf = Align.Center;
            nextLabel.style.position = Position.Relative;
            nextLabel.style.top = -1f;
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
