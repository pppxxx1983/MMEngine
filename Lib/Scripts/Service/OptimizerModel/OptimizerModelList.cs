using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    public class OptimizerModelList : Service
    {
        [SharedRef("oopTragets")]
        public TransformListVar target ;
        public bool openOptimization = true;
        private void OnEnable()
        {
            OpenOptimizer(openOptimization);
            Next();
        }
        public void OpenOptimizer(bool open)
        {
            // if (!target.Validate(this))
            //     return;

            List<Transform> targets = target.Get();
            if (targets == null || targets.Count == 0)
                return;

            for (int i = 0; i < targets.Count; i++)
            {
                Transform targetTransform = targets[i];
                if (targetTransform == null)
                    continue;

                ChildModelOptimizer oo = targetTransform.GetComponent<ChildModelOptimizer>();
                if (oo == null)
                    continue;

                if (open)
                    oo.EnableOptimization();
                else
                    oo.DisableOptimization();
            }
        }
    }
}






