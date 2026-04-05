using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    public class OptimizerModel : Service
    {
        public TransformVar target;
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

            Transform targetTransform = target.Get();
            if (targetTransform == null)
            {
                Debug.LogError("鑾峰彇瀵硅薄澶辫触",this);
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
                Debug.LogError("鑾峰彇缁勪欢澶辫触",this);
            }
        }
    }
}






