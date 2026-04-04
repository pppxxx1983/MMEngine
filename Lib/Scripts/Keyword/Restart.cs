using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    
    public class Restart : Service
    {
        private void OnEnable()
        {
            NextService();
        }
    }
}
