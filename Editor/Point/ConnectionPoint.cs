using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace PlayableFramework.Editor
{
    internal enum ConnectionPointType
    {
        Enter,
        Next,
        Input,
        Output
    }

    internal enum ConnectionPointState
    {
        Default,
        Invalid,
        Valid,
        Connected
    }

    /// <summary>
    /// 节点连接点基类，维护通用连接状态与基础绘制能力。
    /// </summary>
    [Serializable]
    internal class ConnectionPoint
    {
        [SerializeField] private ConnectionPointType pointType;
        [FormerlySerializedAs("direction")]
        [SerializeField] private int legacyDirection;
        [SerializeField] private bool useCustomDrawSide;
        [SerializeField] private bool customDrawOnLeft;
        [SerializeField] private int index;
        [SerializeField] private string label;
        [SerializeField] private List<string> connectedNodeIds = new List<string>();
        [NonSerialized] private GraphNode node;

        public ConnectionPoint(GraphNode owner, ConnectionPointType pointType, int index, string customLabel = null)
        {
            node = owner;
            this.pointType = pointType;
            legacyDirection = IsInputType(pointType) ? 0 : 1;
            this.index = Mathf.Max(0, index);
            label = customLabel == null ? GetDefaultLabel(pointType) : customLabel;
        }

        public GraphNode Node
        {
            get { return node; }
        }

        public ConnectionPointType PointType
        {
            get { return pointType; }
        }

        public int Index
        {
            get { return index; }
        }

        public bool IsInputSide
        {
            get { return legacyDirection == 0; }
        }

        public bool IsOutputSide
        {
            get { return !IsInputSide; }
        }

        public bool DrawOnLeft
        {
            get { return useCustomDrawSide ? customDrawOnLeft : IsInputSide; }
        }

        public string Label
        {
            get { return label; }
            set { label = value; }
        }

        public IReadOnlyList<string> ConnectedNodeIds
        {
            get { return connectedNodeIds; }
        }

        public bool IsConnected
        {
            get { return connectedNodeIds != null && connectedNodeIds.Count > 0; }
        }

        public string SingleConnectedNodeId
        {
            get { return IsConnected ? connectedNodeIds[0] : null; }
        }

        public void Attach(GraphNode owner)
        {
            node = owner;
        }

        public void SetDrawSide(bool drawOnLeft)
        {
            useCustomDrawSide = true;
            customDrawOnLeft = drawOnLeft;
        }

        public void UseDefaultDrawSide()
        {
            useCustomDrawSide = false;
        }

        public void SetSingleConnection(string nodeId)
        {
            ClearConnections();
            AddConnection(nodeId);
        }

        public void AddConnection(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return;
            }

            if (connectedNodeIds == null)
            {
                connectedNodeIds = new List<string>();
            }

            if (connectedNodeIds.Contains(nodeId))
            {
                return;
            }

            connectedNodeIds.Add(nodeId);
        }

        public void RemoveConnection(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || connectedNodeIds == null)
            {
                return;
            }

            connectedNodeIds.Remove(nodeId);
        }

        public void ClearConnections()
        {
            if (connectedNodeIds == null)
            {
                connectedNodeIds = new List<string>();
                return;
            }

            connectedNodeIds.Clear();
        }

        public Rect GetCanvasRect(Vector2 canvasOffset)
        {
            if (Node == null)
            {
                return default(Rect);
            }

            return Node.GetPointCanvasRect(this, canvasOffset);
        }

        public Vector2 GetCanvasCenter(Vector2 canvasOffset)
        {
            return GetCanvasRect(canvasOffset).center;
        }

        public bool Contains(Vector2 mousePosition, Vector2 canvasOffset)
        {
            return GetCanvasRect(canvasOffset).Contains(mousePosition);
        }

        public ConnectionPointState GetState(ConnectionPoint draggingPoint)
        {
            GraphManager manager = GraphManager.Instance;
            if (draggingPoint != null)
            {
                if (draggingPoint == this)
                {
                    return ConnectionPointState.Valid;
                }

                if (manager == null)
                {
                    return ConnectionPointState.Invalid;
                }

                return manager.CanConnect(draggingPoint, this)
                    ? ConnectionPointState.Valid
                    : ConnectionPointState.Invalid;
            }

            if (manager == null)
            {
                return ConnectionPointState.Default;
            }

            return manager.IsPointConnected(this)
                ? ConnectionPointState.Connected
                : ConnectionPointState.Default;
        }

        public Color GetColor(ConnectionPoint draggingPoint)
        {
            switch (GetState(draggingPoint))
            {
                case ConnectionPointState.Invalid:
                    return new Color(0.9f, 0.25f, 0.25f, 1f);

                case ConnectionPointState.Valid:
                    return new Color(0.3f, 0.85f, 0.35f, 1f);

                case ConnectionPointState.Connected:
                    if (PointType == ConnectionPointType.Input || PointType == ConnectionPointType.Output)
                    {
                        return new Color(0.95f, 0.82f, 0.28f, 1f);
                    }

                    return new Color(0.25f, 0.55f, 0.95f, 1f);

                default:
                    return new Color(0.55f, 0.55f, 0.55f, 1f);
            }
        }

        public void DrawRow(Rect rowRect, Vector2 canvasOffset, ConnectionPoint draggingPoint)
        {
            if (PointType == ConnectionPointType.Enter || PointType == ConnectionPointType.Next)
            {
                ConnectionPointDrawer.DrawFlowRow(this, rowRect, canvasOffset, draggingPoint);
                return;
            }

            ConnectionPointDrawer.DrawDataRow(this, rowRect, canvasOffset, draggingPoint);
        }

        protected static bool IsInputType(ConnectionPointType type)
        {
            return type == ConnectionPointType.Enter || type == ConnectionPointType.Input;
        }

        protected static string GetDefaultLabel(ConnectionPointType type)
        {
            switch (type)
            {
                case ConnectionPointType.Enter:
                    return "Enter";
                case ConnectionPointType.Next:
                    return "Next";
                case ConnectionPointType.Input:
                    return "Input";
                case ConnectionPointType.Output:
                    return "Output";
                default:
                    return "Point";
            }
        }
    }
}
