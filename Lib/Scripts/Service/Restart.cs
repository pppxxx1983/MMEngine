using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    
    public class Restart : Service,IGroupNode,IGuideNode,IMirrorNode
    {
        [SerializeField] private string enterId;
        [SerializeField] private string nextId;
        [SerializeField] private bool isMirror=true;
        [SerializeField] private Service enterService;
        [SerializeField] private Service nextService;

        public string GroupParentName => "Group";

        public string EnterId { get => enterId; set => enterId = value; }
        public string NextId { get => nextId; set => nextId = value; }
        public bool IsMirror { get => isMirror; set => isMirror = value; }
        public Service EnterService { get => enterService; set => enterService = value; }
        public Service NextService { get => nextService; set => nextService = value; }


    }
}
