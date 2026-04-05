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
        private void OnEnable()
        {
            if (oopTargets.Count == 0)
            {
                Debug.LogError("SetScaleByList 鍙傛暟涓嶅叏");
                Next();
                return;
            }
            for (int i = 0; i < oopTargets.Count; i++)
            {
                if(oopTargets[i] ==  null)continue;
                oopTargets[i].localScale = targetScale;
            }
            Next();
        }
    }
}






