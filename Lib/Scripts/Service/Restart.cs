using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    public class Restart : Service, IGroupNode, IMirrorNode
    {
        [HideInInspector][SerializeField] private bool isMirror = true;

        public string GroupParentName => "Group";

        public bool IsMirror { get => isMirror; set => isMirror = value; }
    }
}
