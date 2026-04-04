using System.Collections.Generic;
using UnityEngine;

namespace SP
{
    public class ScaleAnimator : Service
    {
        [SharedRef("oopTargets")]
        public TransformListVar source;
        public Vector3 originalScale = Vector3.zero;
        public Vector3 targetScale = Vector3.one;
        public float duration = 0.5f;

        private float currentTime;
        private List<Transform> _targets;

        public override void Enter()
        {
            currentTime = 0f;
            // if (!source.Validate(this))
            // {
            //     Debug.LogError("ScaleAnimatorByList 参数不全", this);
            //     NextService();
            //     return;
            // }

            _targets = source.Get();
            if (_targets == null || _targets.Count == 0)
            {
                Debug.LogError("ScaleAnimatorByList 参数不全", this);
                NextService();
            }
        }

        public override void Update()
        {
            if (_targets == null || _targets.Count == 0)
            {
                NextService();
                return;
            }

            currentTime += Time.deltaTime;
            float progress = Mathf.Clamp01(currentTime / duration);
            progress = Mathf.SmoothStep(0f, 1f, progress);

            for (int i = 0; i < _targets.Count; i++)
            {
                Transform target = _targets[i];
                if (target == null)
                    continue;

                target.localScale = Vector3.Lerp(originalScale, targetScale, progress);
            }

            if (progress >= 1f)
            {
                NextService();
            }
        }
    }
}

