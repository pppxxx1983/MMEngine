using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    public class OptimizerModel : Service
    {
        public TransformVar target;
        public bool openOptimization = true;
        public override void Enter()
        {
            OpenOptimizer(openOptimization);
            NextService();
        }

        public void OpenOptimizer(bool open)
        {
            // if (!target.Validate(this))
            //     return;

            Transform targetTransform = target.Get();
            if (targetTransform == null)
            {
                Debug.LogError("获取对象失败",this);
                return;
            }

            ChildModelOptimizer oo = targetTransform.GetComponent<ChildModelOptimizer>();
            if (oo)
            {
                if (open)
                    oo.EnableOptimization();
                else
                    oo.DisableOptimization();
            }
            else
            {
                Debug.LogError("获取组件失败",this);
            }
        }
    }
}

