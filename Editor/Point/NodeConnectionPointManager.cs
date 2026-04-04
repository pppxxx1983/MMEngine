using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace PlayableFramework.Editor
{
    [Serializable]
    internal sealed class NodeConnectionPointManager
    {
        [FormerlySerializedAs("connectionPoints")]
        [SerializeField] private List<ConnectionPoint> leftConnectionPoints = new List<ConnectionPoint>();
        [SerializeField] private List<ConnectionPoint> rightConnectionPoints = new List<ConnectionPoint>();
        [SerializeField] private ConnectionPoint enterPoint;
        [SerializeField] private ConnectionPoint nextPoint;

        [NonSerialized] private List<ConnectionPoint> allConnectionPoints;

        public ConnectionPoint EnterPoint
        {
            get { return enterPoint; }
        }

        public ConnectionPoint NextPoint
        {
            get { return nextPoint; }
        }

        public IReadOnlyList<ConnectionPoint> InputPoints
        {
            get { return leftConnectionPoints; }
        }

        public void EnsureOwner(GraphNode owner)
        {
            if (leftConnectionPoints == null)
            {
                leftConnectionPoints = new List<ConnectionPoint>();
            }

            if (rightConnectionPoints == null)
            {
                rightConnectionPoints = new List<ConnectionPoint>();
            }

            if (enterPoint != null)
            {
                enterPoint.Attach(owner);
            }

            if (nextPoint != null)
            {
                nextPoint.Attach(owner);
            }

            for (int i = 0; i < leftConnectionPoints.Count; i++)
            {
                if (leftConnectionPoints[i] != null)
                {
                    leftConnectionPoints[i].Attach(owner);
                }
            }

            for (int i = 0; i < rightConnectionPoints.Count; i++)
            {
                if (rightConnectionPoints[i] != null)
                {
                    rightConnectionPoints[i].Attach(owner);
                }
            }
        }

        public void CollectAllPoints(List<ConnectionPoint> output)
        {
            if (output == null)
            {
                return;
            }

            if (enterPoint != null)
            {
                output.Add(enterPoint);
            }

            if (nextPoint != null)
            {
                output.Add(nextPoint);
            }

            for (int i = 0; i < leftConnectionPoints.Count; i++)
            {
                if (leftConnectionPoints[i] != null)
                {
                    output.Add(leftConnectionPoints[i]);
                }
            }

            for (int i = 0; i < rightConnectionPoints.Count; i++)
            {
                if (rightConnectionPoints[i] != null)
                {
                    output.Add(rightConnectionPoints[i]);
                }
            }
        }

        public IReadOnlyList<ConnectionPoint> GetAllPoints()
        {
            if (allConnectionPoints == null)
            {
                allConnectionPoints = new List<ConnectionPoint>();
            }

            allConnectionPoints.Clear();
            CollectAllPoints(allConnectionPoints);
            return allConnectionPoints;
        }

        public int GetRowCount()
        {
            int rootRows = (enterPoint != null || nextPoint != null) ? 1 : 0;
            return Mathf.Max(1, rootRows + leftConnectionPoints.Count + rightConnectionPoints.Count);
        }

        public ConnectionPoint GetLeftPointByRow(int rowIndex)
        {
            if (rowIndex < 0)
            {
                return null;
            }

            bool hasRootRow = enterPoint != null || nextPoint != null;
            if (hasRootRow && rowIndex == 0)
            {
                return enterPoint;
            }

            int rootRows = hasRootRow ? 1 : 0;
            int listIndex = rowIndex - rootRows;
            return listIndex >= 0 && listIndex < leftConnectionPoints.Count ? leftConnectionPoints[listIndex] : null;
        }

        public ConnectionPoint GetRightPointByRow(int rowIndex)
        {
            if (rowIndex < 0)
            {
                return null;
            }

            bool hasRootRow = enterPoint != null || nextPoint != null;
            if (hasRootRow && rowIndex == 0)
            {
                return nextPoint;
            }

            int rootRows = hasRootRow ? 1 : 0;
            int listIndex = rowIndex - rootRows - leftConnectionPoints.Count;
            return listIndex >= 0 && listIndex < rightConnectionPoints.Count ? rightConnectionPoints[listIndex] : null;
        }

        public ConnectionPoint GetPoint(ConnectionPointType type)
        {
            if (type == ConnectionPointType.Enter)
            {
                return enterPoint;
            }

            if (type == ConnectionPointType.Next)
            {
                return nextPoint;
            }

            List<ConnectionPoint> points = IsLeftType(type) ? leftConnectionPoints : rightConnectionPoints;
            for (int i = 0; i < points.Count; i++)
            {
                ConnectionPoint point = points[i];
                if (point != null && point.PointType == type)
                {
                    return point;
                }
            }

            return null;
        }

        public int GetInputPointIndex(ConnectionPoint point)
        {
            if (point == null)
            {
                return -1;
            }

            for (int i = 0; i < leftConnectionPoints.Count; i++)
            {
                if (leftConnectionPoints[i] == point)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool TryGetPointAt(Vector2 mousePosition, Vector2 canvasOffset, out ConnectionPoint point)
        {
            if (enterPoint != null && enterPoint.Contains(mousePosition, canvasOffset))
            {
                point = enterPoint;
                return true;
            }

            if (nextPoint != null && nextPoint.Contains(mousePosition, canvasOffset))
            {
                point = nextPoint;
                return true;
            }

            for (int i = 0; i < leftConnectionPoints.Count; i++)
            {
                ConnectionPoint current = leftConnectionPoints[i];
                if (current != null && current.Contains(mousePosition, canvasOffset))
                {
                    point = current;
                    return true;
                }
            }

            for (int i = 0; i < rightConnectionPoints.Count; i++)
            {
                ConnectionPoint current = rightConnectionPoints[i];
                if (current != null && current.Contains(mousePosition, canvasOffset))
                {
                    point = current;
                    return true;
                }
            }

            point = null;
            return false;
        }

        public void RemoveNodeIdFromAllPoints(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return;
            }

            if (enterPoint != null)
            {
                enterPoint.RemoveConnection(nodeId);
            }

            if (nextPoint != null)
            {
                nextPoint.RemoveConnection(nodeId);
            }

            for (int i = 0; i < leftConnectionPoints.Count; i++)
            {
                if (leftConnectionPoints[i] != null)
                {
                    leftConnectionPoints[i].RemoveConnection(nodeId);
                }
            }

            for (int i = 0; i < rightConnectionPoints.Count; i++)
            {
                if (rightConnectionPoints[i] != null)
                {
                    rightConnectionPoints[i].RemoveConnection(nodeId);
                }
            }
        }

        public void ClearAllConnections()
        {
            if (enterPoint != null)
            {
                enterPoint.ClearConnections();
            }

            if (nextPoint != null)
            {
                nextPoint.ClearConnections();
            }

            for (int i = 0; i < leftConnectionPoints.Count; i++)
            {
                if (leftConnectionPoints[i] != null)
                {
                    leftConnectionPoints[i].ClearConnections();
                }
            }

            for (int i = 0; i < rightConnectionPoints.Count; i++)
            {
                if (rightConnectionPoints[i] != null)
                {
                    rightConnectionPoints[i].ClearConnections();
                }
            }
        }

        public void BuildPoints(
            GraphNode owner,
            bool includeEnter,
            bool includeNext,
            bool includeOutput,
            string enterTypeLabel,
            string nextTypeLabel,
            IReadOnlyList<string> inputTypeLabels,
            string outputTypeLabel)
        {
            leftConnectionPoints.Clear();
            rightConnectionPoints.Clear();
            enterPoint = includeEnter ? new ConnectionPoint(owner, ConnectionPointType.Enter, 0, enterTypeLabel) : null;
            nextPoint = includeNext ? new ConnectionPoint(owner, ConnectionPointType.Next, 0, nextTypeLabel) : null;
            int rootRows = (includeEnter || includeNext) ? 1 : 0;

            if (inputTypeLabels != null && inputTypeLabels.Count > 0)
            {
                for (int i = 0; i < inputTypeLabels.Count; i++)
                {
                    leftConnectionPoints.Add(new ConnectionPoint(owner, ConnectionPointType.Input, leftConnectionPoints.Count + rootRows, inputTypeLabels[i]));
                }
            }

            if (includeOutput)
            {
                rightConnectionPoints.Add(new ConnectionPoint(owner, ConnectionPointType.Output, rootRows + leftConnectionPoints.Count + rightConnectionPoints.Count, outputTypeLabel));
            }
        }

        private static bool IsLeftType(ConnectionPointType type)
        {
            return type == ConnectionPointType.Enter || type == ConnectionPointType.Input;
        }
    }
}
