using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    
    public class SetScaleByList : Service
    {
        
        [SharedRef("oopTargets")]
        public List<Transform> oopTargets = new List<Transform>();
        public Vector3 targetScale = Vector3.one;
        public override void Enter()
        {
            if (oopTargets.Count == 0)
            {
                Debug.LogError("SetScaleByList 参数不全");
                NextService();
                return;
            }
            for (int i = 0; i < oopTargets.Count; i++)
            {
                if(oopTargets[i] ==  null)continue;
                oopTargets[i].localScale = targetScale;
            }
            NextService();
        }
    }
}

