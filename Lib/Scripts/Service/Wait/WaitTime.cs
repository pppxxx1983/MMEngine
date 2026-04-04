using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    
    public class WaitTime : Service
    {
        public float waitTime = 0.1f;
        private float _time=0f;

        public override void Enter()
        {
            _time = 0;
        }

        public override void Update()
        {
            _time += Time.deltaTime;
            if (_time >= waitTime)
            {
                NextService();
            }
        }
    }
}

