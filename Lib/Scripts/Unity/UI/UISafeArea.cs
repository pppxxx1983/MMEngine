using UnityEngine;

namespace SP
{
    /// <summary>
    /// UI鎺у埗鍦ㄥ畨鍏ㄥ尯鍩熷唴
    /// </summary>
    public class UISafeArea : MonoBehaviour
    {
        private void Start()
        {
            SetUIInScreenSafeArea();
        }

        /// <summary>
        /// 璋冩暣UI鍦ㄥ睆骞曠殑瀹夊叏鍖哄煙
        /// </summary>
        private void SetUIInScreenSafeArea()
        {
            Rect rect = Screen.safeArea;
            RectTransform main = transform.GetComponent<RectTransform>();
            // 璁＄畻椤堕儴鍜屽簳閮ㄧ殑鍋忕Щ閲?
            float topOffset = rect.yMax - Screen.height;
            float rightOffset = rect.xMax - Screen.width;
            // 璁剧疆 UI 鍏冪礌鐨勫亸绉婚噺
            main.offsetMin = new Vector2(rect.xMin, rect.yMin);
            main.offsetMax = new Vector2(rightOffset, topOffset);
        }
    }
}

