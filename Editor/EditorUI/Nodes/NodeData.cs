using System;
using UnityEngine;

namespace PlayableFramework.Editor
{
    public enum NodeBorderState
    {
        Default,
        Selected,
        Running,
        Completed
    }

    [Serializable]
    public sealed class NodeData
    {
        public string Id { get; }
        public string Title { get; set; }
        public Vector2 Position { get; set; }
        public bool IsSelected { get; set; }
        public NodeBorderState BorderState { get; set; }
        public NodeData(Vector2 position, string title = "Node", string id = null)
        {
            Id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id;
            Title = string.IsNullOrEmpty(title) ? "Node" : title;
            Position = position;
            BorderState = NodeBorderState.Default;
            // Size = size ?? DefaultNodeSize;
        }
    }
}
