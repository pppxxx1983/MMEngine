using UnityEngine;

namespace SP
{
    /// <summary>
    /// UI控制在安全区域内
    /// </summary>
    public class UISafeArea : MonoBehaviour
    {
        private void Start()
        {
            SetUIInScreenSafeArea();
        }

        /// <summary>
        /// 调整UI在屏幕的安全区域
        /// </summary>
        private void SetUIInScreenSafeArea()
        {
            Rect rect = Screen.safeArea;
            RectTransform main = transform.GetComponent<RectTransform>();
            // 计算顶部和底部的偏移量
            float topOffset = rect.yMax - Screen.height;
            float rightOffset = rect.xMax - Screen.width;
            // 设置 UI 元素的偏移量
            main.offsetMin = new Vector2(rect.xMin, rect.yMin);
            main.offsetMax = new Vector2(rightOffset, topOffset);
        }
    }
}
