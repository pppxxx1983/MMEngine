using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public enum LinkPointType
    {
        Enter,
        Next,
        Input,
        Output,
        RefEnter,
        RefNext
    }

    public class LinkPoint : HLayout
    {
        private static readonly Color DefaultColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        private static readonly Color ValidColor = new Color(0.3f, 0.85f, 0.35f, 1f);
        private static readonly Color InvalidColor = new Color(0.9f, 0.25f, 0.25f, 1f);
        private static readonly Color ConnectedColor = new Color(0.95f, 0.82f, 0.28f, 1f);

        private readonly LinkPointType type;
        private readonly VisualElement leftSlot;
        private readonly Point point;
        private readonly VisualElement rightSlot;
        private VisualElement leftPlaceholder;
        private VisualElement rightPlaceholder;

        public LinkPoint(LinkPointType type)
        {
            this.type = type;
            pickingMode = PickingMode.Position;
            style.justifyContent = Justify.Center;
            style.alignItems = Align.Center;
            style.flexDirection = FlexDirection.Row;

            leftSlot = new VisualElement();
            leftSlot.style.flexShrink = 0f;
            leftSlot.style.justifyContent = Justify.FlexStart;
            leftSlot.style.alignItems = Align.Center;
            leftSlot.style.flexDirection = FlexDirection.Row;

            point = new Point();
            point.RegisterCallback<PointerDownEvent>(OnPointPointerDown, TrickleDown.TrickleDown);

            rightSlot = new VisualElement();
            rightSlot.style.justifyContent = Justify.FlexEnd;
            rightSlot.style.alignItems = Align.Center;
            rightSlot.style.flexDirection = FlexDirection.Row;

            Add(leftSlot);
            Add(rightSlot);
            SetReverseOrder(true);
            SetState(LinkPointState.Default);
        }

        public void SetReverseOrder(bool reverse)
        {
            AddMirrorPlaceholder();
            leftSlot.Clear();
            rightSlot.Clear();

            if (reverse)
            {
                leftSlot.Add(point);
            }
            else
            {
                rightSlot.Add(point);
            }

            RemoveMirrorPlaceholder();
        }

        public Vector2 GetPointWorldPosition()
        {
            return point.worldBound.center;
        }

        public LinkPointType Type => type;
        public string FieldName { get; set; }
        public System.Type ValueType { get; set; }
        public bool ExpectsList { get; set; }
        public string NodeId { get; set; }

        public void SetState(LinkPointState state)
        {
            if (state == LinkPointState.Valid)
            {
                point.SetColor(ValidColor);
                return;
            }

            if (state == LinkPointState.Invalid)
            {
                point.SetColor(InvalidColor);
                return;
            }

            if (state == LinkPointState.Connected)
            {
                point.SetColor(ConnectedColor);
                return;
            }

            point.SetColor(DefaultColor);
        }

        private void AddMirrorPlaceholder()
        {
            if (leftPlaceholder == null)
            {
                leftPlaceholder = CreatePlaceholder();
            }

            if (rightPlaceholder == null)
            {
                rightPlaceholder = CreatePlaceholder();
            }

            if (leftPlaceholder.parent == null)
            {
                leftSlot.Add(leftPlaceholder);
            }

            if (rightPlaceholder.parent == null)
            {
                rightSlot.Add(rightPlaceholder);
            }
        }

        private void RemoveMirrorPlaceholder()
        {
            leftPlaceholder?.RemoveFromHierarchy();
            rightPlaceholder?.RemoveFromHierarchy();
        }

        private static VisualElement CreatePlaceholder()
        {
            VisualElement placeholder = new VisualElement();
            placeholder.style.width = 14f;
            placeholder.style.height = 14f;
            placeholder.style.minWidth = 14f;
            placeholder.style.minHeight = 14f;
            placeholder.style.visibility = Visibility.Hidden;
            placeholder.pickingMode = PickingMode.Ignore;
            return placeholder;
        }

        private void OnPointPointerDown(PointerDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
            {
                return;
            }

            UIManager.Instance.BeginLine(this, GetPointWorldPosition());
            evt.StopPropagation();
        }
    }

    public enum LinkPointState
    {
        Default,
        Invalid,
        Valid,
        Connected
    }
}
