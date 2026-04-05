using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    
    public class Wizard : Service,IGroupNode
    {
        public string GroupParentName => "Group";

        private void OnEnable()
        {
            Next();
        }
    }
}






