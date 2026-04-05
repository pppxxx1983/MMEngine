using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    
    public class Wizard : Service,IGroupNode
    {
        private void OnEnable()
        {
            Next();
        }
    }
}






