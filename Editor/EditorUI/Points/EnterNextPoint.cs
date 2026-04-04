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
            leftLayout.Clear();
            rightLayout.Clear();

            if (mirror)
            {
                enterLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                nextLabel.style.unityTextAlign = TextAnchor.MiddleLeft;

                enterPoint.SetReverseOrder(false);
                nextPoint.SetReverseOrder(true);

                leftLayout.Add(nextPoint);
                leftLayout.Add(nextLabel);

                rightLayout.Add(enterLabel);
                rightLayout.Add(enterPoint);
            }
            else
            {
                enterLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                nextLabel.style.unityTextAlign = TextAnchor.MiddleRight;

                enterPoint.SetReverseOrder(true);
                nextPoint.SetReverseOrder(false);

                leftLayout.Add(enterPoint);
                leftLayout.Add(enterLabel);

                rightLayout.Add(nextLabel);
                rightLayout.Add(nextPoint);
            }
        }

        public LinkPoint EnterPoint => enterPoint;

        public LinkPoint NextPoint => nextPoint;
    }
}
