using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    
    public class WaitTime : Service
    {
        public float waitTime = 0.1f;
        private float _time=0f;
        private void OnEnable()
        {
            _time = 0;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            if (_time >= waitTime)
            {
                Next();
            }
        }
    }
}






