using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public sealed class RefEnterNextPoint : HLayout
    {
        private const float RowHeight = 16f;
        private const float SlotFontSize = 8f;
        // RefEnter 与 Enter 同色，RefNext 与 Next 同色
        private static readonly Color RefEnterPlateColor = new Color(0.18f, 0.42f, 0.31f, 0.95f);  // 绿色
        private static readonly Color RefNextPlateColor = new Color(0.23f, 0.36f, 0.58f, 0.95f);   // 蓝色

        private readonly LinkPoint refEnterPoint;
        private readonly Label refEnterLabel;
        private readonly HLayout leftLayout;
        private readonly LinkPoint refNextPoint;
        private readonly Label refNextLabel;
        private readonly HLayout rightLayout;

        public RefEnterNextPoint()
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

            refEnterPoint = new LinkPoint(LinkPointType.RefEnter);
            refEnterPoint.SetReverseOrder(true);
            refEnterPoint.style.flexGrow = 0f;
            refEnterPoint.style.flexShrink = 0f;
            refEnterPoint.style.alignSelf = Align.Center;
            leftLayout.Add(refEnterPoint);

            refEnterLabel = new Label("RefEnter");
            refEnterLabel.style.height = RowHeight;
            refEnterLabel.style.minHeight = RowHeight;
            refEnterLabel.style.fontSize = SlotFontSize;
            refEnterLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            refEnterLabel.style.alignSelf = Align.Center;
            refEnterLabel.style.position = Position.Relative;
            refEnterLabel.style.top = -1f;
            refEnterLabel.style.backgroundColor = RefEnterPlateColor;
            refEnterLabel.style.borderTopLeftRadius = 3f;
            refEnterLabel.style.borderTopRightRadius = 3f;
            refEnterLabel.style.borderBottomLeftRadius = 3f;
            refEnterLabel.style.borderBottomRightRadius = 3f;
            refEnterLabel.style.paddingLeft = 6f;
            refEnterLabel.style.paddingRight = 6f;
            refEnterLabel.style.marginLeft = 4f;
            leftLayout.Add(refEnterLabel);

            rightLayout = new HLayout();
            rightLayout.style.flexGrow = 1f;
            rightLayout.style.flexShrink = 1f;
            rightLayout.style.justifyContent = Justify.FlexEnd;
            rightLayout.style.alignItems = Align.Center;
            rightLayout.style.height = RowHeight;
            rightLayout.style.minHeight = RowHeight;
            rightLayout.style.maxHeight = RowHeight;
            Add(rightLayout);

            refNextLabel = new Label("RefNext");
            refNextLabel.style.height = RowHeight;
            refNextLabel.style.minHeight = RowHeight;
            refNextLabel.style.fontSize = SlotFontSize;
            refNextLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            refNextLabel.style.alignSelf = Align.Center;
            refNextLabel.style.position = Position.Relative;
            refNextLabel.style.top = -1f;
            refNextLabel.style.backgroundColor = RefNextPlateColor;
            refNextLabel.style.borderTopLeftRadius = 3f;
            refNextLabel.style.borderTopRightRadius = 3f;
            refNextLabel.style.borderBottomLeftRadius = 3f;
            refNextLabel.style.borderBottomRightRadius = 3f;
            refNextLabel.style.paddingLeft = 6f;
            refNextLabel.style.paddingRight = 6f;
            refNextLabel.style.marginRight = 4f;
            rightLayout.Add(refNextLabel);

            refNextPoint = new LinkPoint(LinkPointType.RefNext);
            refNextPoint.SetReverseOrder(false);
            refNextPoint.style.flexGrow = 0f;
            refNextPoint.style.flexShrink = 0f;
            refNextPoint.style.alignSelf = Align.Center;
            rightLayout.Add(refNextPoint);

            SetMirror(false);
        }

        public void SetMirror(bool mirror)
        {
            if (mirror)
            {
                style.flexDirection = FlexDirection.RowReverse;
                refEnterLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                refNextLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                leftLayout.style.flexDirection = FlexDirection.RowReverse;
                rightLayout.style.flexDirection = FlexDirection.RowReverse;
                leftLayout.style.justifyContent = Justify.FlexStart;
                rightLayout.style.justifyContent = Justify.FlexEnd;
                refEnterPoint.SetReverseOrder(true);
                refNextPoint.SetReverseOrder(true);
            }
            else
            {
                style.flexDirection = FlexDirection.Row;
                refEnterLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                refNextLabel.style.unityTextAlign = TextAnchor.MiddleRight;
                leftLayout.style.flexDirection = FlexDirection.Row;
                rightLayout.style.flexDirection = FlexDirection.Row;
                leftLayout.style.justifyContent = Justify.FlexStart;
                rightLayout.style.justifyContent = Justify.FlexEnd;
                refEnterPoint.SetReverseOrder(true);
                refNextPoint.SetReverseOrder(false);
            }
        }

        public LinkPoint RefEnterPoint => refEnterPoint;

        public LinkPoint RefNextPoint => refNextPoint;
    }
}
