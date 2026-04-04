using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SP
{
    public class GetGlobalListToSource : Service
    {
        [Input] public TransformListVar globalData;
        
        [Output]
        private List<Transform> output;

        public override void Enter()
        {
            if (!globalData.ValidateAndLog(this))
            {
                return;
            }

            output = globalData.Get();
            if (output == null)
            {
                Debug.LogWarning("GetGlobalListByIndex called with no list transform", this);
                return;
            }

            NextService();
        }

    }
}

