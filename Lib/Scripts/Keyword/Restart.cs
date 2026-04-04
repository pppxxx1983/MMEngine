using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    
    public class Restart : Service
    {
        protected override void OnEnable()
        {
            NextService();
        }
    }
}
