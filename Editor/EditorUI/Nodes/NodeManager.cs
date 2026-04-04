using System.Collections.Generic;
using UnityEngine;

namespace PlayableFramework.Editor
{
    public sealed class NodeManager
    {
        private static NodeManager instance;
        private readonly List<NodeData> nodes = new List<NodeData>();

        public static NodeManager Instance => instance ??= new NodeManager();

        public event System.Action Changed;
        public event System.Action SelectionChanged;
        public event System.Action PosChanged;

        public IReadOnlyList<NodeData> Nodes => nodes;

        public NodeData SelectedNode { get; private set; }

        public List<NodeData> GetSelectedNodes()
        {
            List<NodeData> selectedNodes = new List<NodeData>();
            for (int i = 0; i < nodes.Count; i++)
            {
                NodeData node = nodes[i];
                if (node != null && node.IsSelected)
                {
                    selectedNodes.Add(node);
                }
            }

            return selectedNodes;
        }

        private NodeManager()
        {
            
        }

        public NodeData CreateNode(Vector2 position, string title = "Node", string id = null)
        {
            NodeData node = new NodeData(position, title, id);
            nodes.Add(node);
            NotifyChanged();
            return node;
        }

        public bool RemoveNode(NodeData node)
        {
            if (node == null)
            {
                return false;
            }

            if (SelectedNode == node)
            {
                ClearSelection();
            }

            bool removed = nodes.Remove(node);
            if (removed)
            {
                NotifyChanged();
            }

            return removed;
        }

        public NodeData GetNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return null;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                NodeData node = nodes[i];
                if (node != null && node.Id == nodeId)
                {
                    return node;
                }
            }

            return null;
        }

        public void SelectNode(NodeData node)
        {
            List<NodeData> selectedNodes = new List<NodeData>();
            if (node != null)
            {
                selectedNodes.Add(node);
            }

            SetSelection(selectedNodes);
        }

        public void ClearSelection()
        {
            SetSelection(null);
        }

        public void Clear()
        {
            nodes.Clear();
            SelectedNode = null;
            NotifyChanged();
        }

        public void SetNodes(List<NodeData> newNodes)
        {
            HashSet<string> selectedIds = new HashSet<string>();
            for (int i = 0; i < nodes.Count; i++)
            {
                NodeData current = nodes[i];
                if (current != null && current.IsSelected && !string.IsNullOrEmpty(current.Id))
                {
                    selectedIds.Add(current.Id);
                }
            }

            nodes.Clear();

            if (newNodes != null)
            {
                nodes.AddRange(newNodes);
            }

            SelectedNode = null;
            for (int i = 0; i < nodes.Count; i++)
            {
                NodeData node = nodes[i];
                if (node == null)
                {
                    continue;
                }

                bool isSelected = !string.IsNullOrEmpty(node.Id) && selectedIds.Contains(node.Id);
                node.IsSelected = isSelected;
                if (isSelected && SelectedNode == null)
                {
                    SelectedNode = node;
                }
            }

            NotifyChanged();
            NotifySelectionChanged();
        }

        public void SetSelection(List<NodeData> selectedNodes)
        {
            HashSet<string> selectedIds = new HashSet<string>();
            if (selectedNodes != null)
            {
                for (int i = 0; i < selectedNodes.Count; i++)
                {
                    NodeData node = selectedNodes[i];
                    if (node != null && !string.IsNullOrEmpty(node.Id))
                    {
                        selectedIds.Add(node.Id);
                    }
                }
            }

            SelectedNode = null;
            for (int i = 0; i < nodes.Count; i++)
            {
                NodeData node = nodes[i];
                if (node == null)
                {
                    continue;
                }

                node.IsSelected = !string.IsNullOrEmpty(node.Id) && selectedIds.Contains(node.Id);
                if (node.IsSelected && SelectedNode == null)
                {
                    SelectedNode = node;
                }
            }

            NotifySelectionChanged();
        }

        public void NotifyPosChanged()
        {
            PosChanged?.Invoke();
        }


        private void NotifyChanged()
        {
            Changed?.Invoke();
        }

        private void NotifySelectionChanged()
        {
            SelectionChanged?.Invoke();
        }
    }
}
