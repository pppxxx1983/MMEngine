using System;
using System.Collections.Generic;
using System.Reflection;
using SP;
using UnityEditor;
using UnityEngine;

namespace PlayableFramework.Editor
{
    [Serializable]
    internal sealed class GraphNode
    {
        public const float Width = 220f;
        public const float PortSize = 16f;
        public const float TitleHeight = 22f;
        public const float RowHeight = 20f;
        public const float ContentPadding = 6f;
        public const float InputSlotReservedWidth = 88f;
        private static readonly Color NodeBackgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f);
        private static readonly Color NodeSelectedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        private static readonly Color TitleColor = new Color(0.95f, 0.95f, 0.95f, 1f);

        public string Id;
        public string Title;
        public Vector2 Position;
        public Type ServiceType;
        [NonSerialized] public bool IsSelected;

        [SerializeField] private NodeConnectionPointManager pointManager = new NodeConnectionPointManager();
        [SerializeField] private List<string> inputFieldNames = new List<string>();
        [NonSerialized] private GraphNodeGameObjectLogic gameObjectLogic = new GraphNodeGameObjectLogic();

        public GraphNode(Vector2 position, Type type)
        {
            ServiceType = type;
            Id = Guid.NewGuid().ToString("N");
            Title = type != null ? type.Name : "Node";
            Position = position;
            pointManager.BuildPoints(this, true, true, false, null, null, null, null);
            gameObjectLogic = new GraphNodeGameObjectLogic();
        }

        private GraphNodeGameObjectLogic GameObjectLogic
        {
            get
            {
                if (gameObjectLogic == null)
                {
                    gameObjectLogic = new GraphNodeGameObjectLogic();
                }

                return gameObjectLogic;
            }
        }

        private void EnsurePointOwner()
        {
            if (pointManager != null)
            {
                pointManager.EnsureOwner(this);
            }
        }

        public void InitConnectionPoints(GameObject nodeObject = null)
        {
            if (pointManager == null)
            {
                pointManager = new NodeConnectionPointManager();
            }

            pointManager.EnsureOwner(this);

            ServiceType = GameObjectLogic.ResolveServiceType(nodeObject, ServiceType);
            List<FieldInfo> inputFields = GraphNodeUtility.FindTaggedFields(ServiceType, typeof(InputAttribute));
            FieldInfo outputField = GraphNodeUtility.FindTaggedField(ServiceType, typeof(OutputAttribute));
            bool hasOutputPoint = outputField != null;
            string outputTypeLabel = outputField != null ? GraphNodeUtility.GetTypeDisplayName(outputField.FieldType) : string.Empty;
            bool hasEnterPoint;
            bool hasNextPoint;
            GameObjectLogic.ResolveRootPortFlags(nodeObject, ServiceType, out hasEnterPoint, out hasNextPoint);
            inputFieldNames.Clear();
            List<string> inputLabels = new List<string>();
            for (int i = 0; i < inputFields.Count; i++)
            {
                inputFieldNames.Add(inputFields[i].Name);
                inputLabels.Add(GraphNodeUtility.GetInputTypeLabel(inputFields[i]));
            }
            pointManager.BuildPoints(
                this,
                hasEnterPoint,
                hasNextPoint,
                hasOutputPoint,
                null,
                null,
                inputLabels,
                outputTypeLabel);
        }

        public IReadOnlyList<ConnectionPoint> ConnectionPoints
        {
            get
            {
                EnsurePointOwner();
                return pointManager != null ? pointManager.GetAllPoints() : new List<ConnectionPoint>();
            }
        }

        public ConnectionPoint EnterPoint
        {
            get
            {
                EnsurePointOwner();
                return pointManager != null ? pointManager.EnterPoint : null;
            }
        }

        public ConnectionPoint NextPoint
        {
            get
            {
                EnsurePointOwner();
                return pointManager != null ? pointManager.NextPoint : null;
            }
        }

        public ConnectionPoint DataInputPoint
        {
            get { return GetPoint(ConnectionPointType.Input); }
        }

        public IReadOnlyList<ConnectionPoint> DataInputPoints
        {
            get
            {
                EnsurePointOwner();
                return pointManager != null ? pointManager.InputPoints : new List<ConnectionPoint>();
            }
        }

        public ConnectionPoint DataOutputPoint
        {
            get { return GetPoint(ConnectionPointType.Output); }
        }

        public Rect GetLocalRect()
        {
            return new Rect(Position.x, Position.y, Width, GetNodeHeight());
        }

        public Rect GetCanvasRect(Vector2 canvasOffset)
        {
            Rect rect = GetLocalRect();
            rect.position += canvasOffset;
            return rect;
        }

        public string GetDisplayTitle()
        {
            if (!string.IsNullOrEmpty(Title))
            {
                return Title;
            }

            if (ServiceType != null)
            {
                return ServiceType.Name;
            }

            return "Node";
        }

        public void Render(
            Vector2 canvasOffset,
            ConnectionPoint draggingConnectionPoint)
        {
        
            RenderInternal( canvasOffset, draggingConnectionPoint);
        }

        private void RenderInternal(Vector2 canvasOffset, ConnectionPoint draggingConnectionPoint)
        {
            Rect rect = GetCanvasRect(canvasOffset);
            Color bg = IsSelected ? NodeSelectedColor : NodeBackgroundColor;
            EditorGUI.DrawRect(rect, bg);
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            DrawTitle(rect);

            int rowCount = pointManager != null ? pointManager.GetRowCount() : 1;
            for (int i = 0; i < rowCount; i++)
            {
                Rect rowRect = GetRowRect(i, canvasOffset);
                ConnectionPoint leftPoint = pointManager != null ? pointManager.GetLeftPointByRow(i) : null;
                ConnectionPoint rightPoint = pointManager != null ? pointManager.GetRightPointByRow(i) : null;

                if (leftPoint != null)
                {
                    leftPoint.DrawRow(rowRect, canvasOffset, draggingConnectionPoint);
                }

                if (rightPoint != null)
                {
                    rightPoint.DrawRow(rowRect, canvasOffset, draggingConnectionPoint);
                }
            }
        }

        private void DrawTitle(Rect rect)
        {
            Rect titleRect = new Rect(rect.x + 8f, rect.y + 2f, rect.width - 16f, TitleHeight - 4f);
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = TitleColor;
            style.alignment = TextAnchor.MiddleCenter;
            style.clipping = TextClipping.Clip;
            GUI.Label(titleRect, GetDisplayTitle(), style);
        }

        public int GetPointRowIndex(ConnectionPoint point)
        {
            if (point == null)
            {
                return 0;
            }

            return Mathf.Max(0, point.Index);
        }

        public Rect GetPointCanvasRect(ConnectionPoint point, Vector2 canvasOffset)
        {
            if (point == null)
            {
                return default(Rect);
            }

            int rowCount = pointManager != null ? pointManager.GetRowCount() : 1;
            int rowIndex = Mathf.Min(GetPointRowIndex(point), Mathf.Max(0, rowCount - 1));
            Rect rowRect = GetRowRect(rowIndex, canvasOffset);
            float y = rowRect.center.y - PortSize * 0.5f;
            float x = point.IsInputSide ? rowRect.x : rowRect.xMax - PortSize;
            return new Rect(x, y, PortSize, PortSize);
        }

        private Rect GetRowRect(int rowIndex, Vector2 canvasOffset)
        {
            Rect nodeRect = GetCanvasRect(canvasOffset);
            float y = nodeRect.y + TitleHeight + ContentPadding + rowIndex * RowHeight;
            return new Rect(nodeRect.x + ContentPadding, y, nodeRect.width - ContentPadding * 2f, RowHeight);
        }

        private float GetNodeHeight()
        {
            int rowCount = pointManager != null ? pointManager.GetRowCount() : 1;
            return TitleHeight + ContentPadding * 2f + rowCount * RowHeight;
        }

        public ConnectionPoint GetPoint(ConnectionPointType type)
        {
            EnsurePointOwner();
            return pointManager != null ? pointManager.GetPoint(type) : null;
        }

        public int GetInputPointIndex(ConnectionPoint point)
        {
            EnsurePointOwner();
            return pointManager != null ? pointManager.GetInputPointIndex(point) : -1;
        }

        public string GetInputFieldNameByIndex(int index)
        {
            if (index < 0 || index >= inputFieldNames.Count)
            {
                return string.Empty;
            }

            return inputFieldNames[index] ?? string.Empty;
        }

        public string GetOutputFieldName()
        {
            return GameObjectLogic.GetOutputFieldName(this, ServiceType);
        }

        public string GetDataPointObjectName(ConnectionPoint point )
        {
            if (point == null)
            {
                return "None";
            }

            GUIContent content;
            if (point.PointType == ConnectionPointType.Input)
            {
                content = BuildInputValueContent(point);
            }
            else if (point.PointType == ConnectionPointType.Output)
            {
                content = BuildOutputValueContent(point);
            }
            else
            {
                return "None";
            }

            return content != null && !string.IsNullOrEmpty(content.text) ? content.text : "None";
        }

        public bool TryGetPointAt(Vector2 mousePosition, Vector2 canvasOffset, out ConnectionPoint point)
        {
            if (pointManager == null)
            {
                point = null;
                return false;
            }

            EnsurePointOwner();
            return pointManager.TryGetPointAt(mousePosition, canvasOffset, out point);
        }

        public void RemoveNodeIdFromAllPoints(string nodeId)
        {
            if (pointManager != null)
            {
                pointManager.RemoveNodeIdFromAllPoints(nodeId);
            }
        }

        public void ClearAllConnections()
        {
            if (pointManager != null)
            {
                pointManager.ClearAllConnections();
            }
        }

        private void DrawInputTypeSlot(Rect rowRect, ConnectionPoint inputPoint)
        {
            GUIContent content = BuildInputValueContent(inputPoint);
            GUIStyle style = EditorStyles.label;
            float textWidth = style.CalcSize(new GUIContent(content != null ? content.text : string.Empty)).x;
            float slotWidth = Mathf.Clamp(textWidth + 24f, 56f, 84f);
            float nodeLeft = rowRect.x - ContentPadding;
            const float edgeInset = 6f;
            Rect slotRect = new Rect(
                nodeLeft + Width - slotWidth - edgeInset,
                rowRect.y + 2f,
                slotWidth,
                rowRect.height - 4f);

            GUI.Box(slotRect, GUIContent.none, EditorStyles.objectField);
            GUIStyle valueStyle = new GUIStyle(EditorStyles.label);
            valueStyle.alignment = TextAnchor.MiddleLeft;
            valueStyle.clipping = TextClipping.Clip;
            valueStyle.fontSize = 10;
            Rect contentRect = new Rect(slotRect.x + 2f, slotRect.y, slotRect.width - 4f, slotRect.height);
            GUI.Label(contentRect, content != null ? content.text : string.Empty, valueStyle);
        }

        private bool ShouldShowInputSlot(ConnectionPoint inputPoint, GraphManager manager)
        {
            InputType inputType;
            if (!TryGetInputPointInputType(inputPoint, manager, out inputType))
            {
                return true;
            }

            return inputType == InputType.Default;
        }

        private bool TryGetInputPointInputType(ConnectionPoint inputPoint, GraphManager manager, out InputType inputType)
        {
            return GameObjectLogic.TryGetInputPointInputType(this, inputPoint, manager, inputFieldNames, out inputType);
        }

        private void DrawOutputTypeSlot(Rect rowRect, ConnectionPoint outputPoint)
        {
            GUIContent content = BuildOutputValueContent(outputPoint);
            float slotWidth = 84f;
            float nodeLeft = rowRect.x - ContentPadding;
            const float edgeInset = 6f;
            Rect slotRect = new Rect(
                nodeLeft + edgeInset,
                rowRect.y + 2f,
                slotWidth,
                rowRect.height - 4f);

            GUI.Box(slotRect, GUIContent.none, EditorStyles.objectField);
            GraphNodeUtility.DrawRightAlignedName(slotRect, content);
        }

        private GUIContent BuildInputValueContent(ConnectionPoint inputPoint)
        {
            return GameObjectLogic.BuildInputValueContent(this, inputPoint, inputFieldNames);
        }

        private GUIContent BuildOutputValueContent(ConnectionPoint outputPoint)
        {
            return GameObjectLogic.BuildOutputValueContent(this, outputPoint);
        }

    }
}
