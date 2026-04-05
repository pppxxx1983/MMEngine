using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlayableFramework.Editor
{
    public sealed class NodeManager
    {
        private static NodeManager instance;
        private readonly List<UINode> nodes = new List<UINode>();

        public static NodeManager Instance => instance ??= new NodeManager();

        public event System.Action Changed;
        public event System.Action SelectionChanged;
        public event System.Action PosChanged;

        public IReadOnlyList<UINode> UINodes => nodes;

        public UINode SelectedUINode { get; private set; }

        public List<UINode> GetSelectedUINodes()
        {
            List<UINode> selectedNodes = new List<UINode>();
            for (int i = 0; i < nodes.Count; i++)
            {
                UINode node = nodes[i];
                if (node != null && node.Data != null && node.Data.IsSelected)
                {
                    selectedNodes.Add(node);
                }
            }

            return selectedNodes;
        }

        private NodeManager()
        {
            
        }

        public UINode CreateNode(Vector2 position, string title = "Node", string id = null)
        {
            UINode node = new UINode(new NodeData(position, title, id));
            nodes.Add(node);
            NotifyChanged();
            return node;
        }

        public bool RemoveNode(UINode node)
        {
            if (node == null)
            {
                return false;
            }

            if (SelectedUINode == node)
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

        public UINode GetUINode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return null;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                UINode node = nodes[i];
                if (node != null && node.Data != null && node.Data.Id == nodeId)
                {
                    return node;
                }
            }

            return null;
        }

        public LinkPoint GetCurrentMouseLinkPoint()
        {
            VisualElement root = UIManager.Instance.Root;
            if (root == null || root.panel == null)
            {
                return null;
            }

            Vector2 mousePosition = Event.current != null
                ? Event.current.mousePosition
                : GUIUtility.ScreenToGUIPoint(Input.mousePosition);

            VisualElement pickedElement = root.panel.Pick(mousePosition) as VisualElement;
            while (pickedElement != null)
            {
                LinkPoint linkPoint = pickedElement as LinkPoint;
                if (linkPoint != null)
                {
                    return linkPoint;
                }

                pickedElement = pickedElement.parent;
            }

            return null;
        }

        public void SelectNode(UINode node)
        {
            SelectNode(node, false);
        }

        public void SelectNode(UINode node, bool additive)
        {
            if (!additive)
            {
                UIManager.Instance.Curve?.ClearSelection();
                UIManager.Instance.VarLine?.ClearSelection();
            }

            List<UINode> selectedNodes = new List<UINode>();
            if (additive)
            {
                selectedNodes.AddRange(GetSelectedUINodes());
            }

            if (node != null)
            {
                bool alreadyIncluded = false;
                for (int i = 0; i < selectedNodes.Count; i++)
                {
                    if (selectedNodes[i] == node)
                    {
                        alreadyIncluded = true;
                        break;
                    }
                }

                if (!alreadyIncluded)
                {
                    selectedNodes.Add(node);
                }
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
            SelectedUINode = null;
            NotifyChanged();
        }

        public void SetUINodes(List<UINode> newNodes)
        {
            HashSet<string> selectedIds = new HashSet<string>();
            for (int i = 0; i < nodes.Count; i++)
            {
                UINode current = nodes[i];
                if (current != null && current.Data != null && current.Data.IsSelected && !string.IsNullOrEmpty(current.Data.Id))
                {
                    selectedIds.Add(current.Data.Id);
                }
            }

            nodes.Clear();

            if (newNodes != null)
            {
                nodes.AddRange(newNodes);
            }

            SelectedUINode = null;
            for (int i = 0; i < nodes.Count; i++)
            {
                UINode node = nodes[i];
                if (node == null || node.Data == null)
                {
                    continue;
                }

                bool isSelected = !string.IsNullOrEmpty(node.Data.Id) && selectedIds.Contains(node.Data.Id);
                node.Data.IsSelected = isSelected;
                if (isSelected && SelectedUINode == null)
                {
                    SelectedUINode = node;
                }
            }

            NotifyChanged();
            NotifySelectionChanged();
        }

        public void SetSelection(List<UINode> selectedNodes)
        {
            HashSet<string> selectedIds = new HashSet<string>();
            if (selectedNodes != null)
            {
                for (int i = 0; i < selectedNodes.Count; i++)
                {
                    UINode node = selectedNodes[i];
                    if (node != null && node.Data != null && !string.IsNullOrEmpty(node.Data.Id))
                    {
                        selectedIds.Add(node.Data.Id);
                    }
                }
            }

            SelectedUINode = null;
            for (int i = 0; i < nodes.Count; i++)
            {
                UINode node = nodes[i];
                if (node == null || node.Data == null)
                {
                    continue;
                }

                node.Data.IsSelected = !string.IsNullOrEmpty(node.Data.Id) && selectedIds.Contains(node.Data.Id);
                if (node.Data.IsSelected && SelectedUINode == null)
                {
                    SelectedUINode = node;
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
