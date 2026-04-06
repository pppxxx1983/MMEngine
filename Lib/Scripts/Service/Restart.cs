using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    public class Restart : Service, IGroupNode, IMirrorNode, IRefPort, IFlowPort
    {
        public bool HasEnterPort => false;
        public bool HasNextPort => false;
        [HideInInspector] public string EnterId { get; set; }
        [HideInInspector] public string NextId { get; set; }

        /// <summary>
        /// 当目标 Service 调用 Next() 时触发此方法。
        /// </summary>
        public void OnRefActivated(){

        }
        
        [HideInInspector] private bool isMirror = true;

        public string GroupParentName => "Group";

        public bool IsMirror { get => isMirror; set => isMirror = value; }
    }
}
