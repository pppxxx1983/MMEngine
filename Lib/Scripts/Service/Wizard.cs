using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    
    public class Wizard : Service
    {
        public override void Enter()
        {
            NextService();
        }
    }
}

