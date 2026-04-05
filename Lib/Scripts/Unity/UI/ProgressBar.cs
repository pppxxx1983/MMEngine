using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SP
{
    /// <summary>
    /// 进度条
    /// </summary>
    public class ProgressBar : MonoBehaviour,IMMVarTarget
    {
        [Tooltip("进度条")]
        public SpriteRenderer bar;

        [Tooltip("进度条最大高度")]
        public float barMaxHeight;

        [Tooltip("满进度值")]
        public int processMax;

        public TextMeshPro barText;

        private float tempProcess = 0;
        private float currentTarget = 0;
        private Tween tween;

        private void Awake()
        {
            if (bar != null)
            {
                bar.size = new Vector2(bar.size.x, 0);
            }

            ClearProcess();
        }

        public bool IsMax()
        {
            return tempProcess == processMax;
        }

        public void AddProcess(int num)
        {
            tempProcess += num;

            float safeMax = Mathf.Max(processMax, 1);
            float y;

            if (tempProcess >= safeMax)
            {
                y = barMaxHeight;
                tempProcess = safeMax;
            }
            else
            {
                float p = tempProcess / safeMax;
                y = barMaxHeight * p;
            }

            if (tween != null && tween.IsActive())
            {
                tween.Kill();
            }

            tween = DOTween.To(GetCurrent, SetCurrent, y, 0.1f).OnUpdate(() =>
            {
                if (bar != null)
                {
                    bar.size = new Vector2(bar.size.x, currentTarget);
                }
            });

            if (barText != null)
            {
                barText.SetText((safeMax - tempProcess).ToString());
            }
        }

        private float GetCurrent()
        {
            return currentTarget;
        }

        private void SetCurrent(float value)
        {
            currentTarget = value;
        }

        public void ClearProcess()
        {
            if (tween != null && tween.IsActive())
            {
                tween.Kill();
            }

            tween = null;
            currentTarget = 0;
            tempProcess = 0;

            if (bar != null)
            {
                bar.size = new Vector2(bar.size.x, 0);
            }

            if (barText != null)
            {
                barText.SetText(processMax.ToString());
            }
        }

        public void Hide()
        {
            ClearProcess();
            gameObject.SetActive(false);
        }
    }
}
