using System.Collections.Generic;
using UnityEngine;

namespace PlayableFramework.Editor
{
    internal sealed class GraphLayoutAsset : ScriptableObject
    {
        public List<GraphNode> nodes = new List<GraphNode>();
    }
}
