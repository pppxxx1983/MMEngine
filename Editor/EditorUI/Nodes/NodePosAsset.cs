using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayableFramework.Editor
{
    [Serializable]
    public sealed class NodePos
    {
        public string id;
        public Vector2 pos;
    }

    public sealed class NodePosAsset : ScriptableObject
    {
        public List<NodePos> nodes = new List<NodePos>();
    }
}
