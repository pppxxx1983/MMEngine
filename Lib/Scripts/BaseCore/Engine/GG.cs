
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
namespace SP
{
    
    public class gg : SingletonMono<gg>
    {
        /// <summary>
        /// 妫€鏌ュ苟鍒涘缓 EventSystem锛堟牳蹇冩柟娉曪級
        /// </summary>
        public void CreateEventSystemIfNotExists()
        {
            // 1. 鏌ユ壘鍦烘櫙涓槸鍚﹀凡鏈?EventSystem
            EventSystem existingEventSystem = FindObjectOfType<EventSystem>();
        
            // 2. 濡傛灉娌℃湁锛屽垯鍒涘缓
            if (existingEventSystem == null)
            {
                // 鍒涘缓绌?GameObject 骞跺懡鍚?
                GameObject eventSystemGO = new GameObject("EventSystem");
            
                // 3. 娣诲姞鏍稿績鐨?EventSystem 缁勪欢
                EventSystem eventSystem = eventSystemGO.AddComponent<EventSystem>();
            
                // 4. 娣诲姞杈撳叆妯″潡锛堝繀椤伙紒鍚﹀垯 EventSystem 鏃犳硶澶勭悊浠讳綍杈撳叆浜嬩欢锛?
                StandaloneInputModule inputModule = eventSystemGO.AddComponent<StandaloneInputModule>();
            
                // 鍙€夛細璁剧疆杈撳叆妯″潡鐨勫弬鏁帮紙榛樿鍊煎嵆鍙弧瓒冲ぇ閮ㄥ垎闇€姹傦級
                inputModule.horizontalAxis = "Horizontal";
                inputModule.verticalAxis = "Vertical";
                inputModule.submitButton = "Submit";
                inputModule.cancelButton = "Cancel";

                Debug.Log("EventSystem 宸查€氳繃浠ｇ爜鍒涘缓瀹屾垚");
            }
            else
            {
                Debug.Log("鍦烘櫙涓凡瀛樺湪 EventSystem锛屾棤闇€閲嶅鍒涘缓");
            }
        }

        /// <summary>
        /// 鑾峰彇鍦烘櫙涓殑Canvas锛岃嫢涓嶅瓨鍦ㄥ垯鍒涘缓骞惰繑鍥?
        /// </summary>
        /// <returns>鍦烘櫙涓殑Canvas缁勪欢锛堢‘淇濋潪null锛?/returns>
        public Canvas GetOrCreateCanvas()
        {
            // 1. 鏌ユ壘鍦烘櫙涓凡鏈夌殑Canvas锛堜紭鍏堟壘鍚敤鐨勶級
            Canvas existingCanvas = FindObjectOfType<Canvas>();

            // 2. 濡傛灉鎵惧埌锛岀洿鎺ヨ繑鍥?
            if (existingCanvas != null)
            {
                return existingCanvas;
            }

            // 3. 鏈壘鍒板垯鍒涘缓鏂癈anvas
            Debug.Log("鏈壘鍒癈anvas锛屽紑濮嬪垱寤烘柊Canvas");

            // 鍒涘缓Canvas GameObject
            GameObject canvasGO = new GameObject("Canvas");
            Canvas newCanvas = canvasGO.AddComponent<Canvas>();

            // 鏍稿績閰嶇疆1锛氳缃负灞忓箷绌洪棿-鍙犲姞锛堟渶甯哥敤鐨刄I娓叉煋妯″紡锛?
            newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            newCanvas.pixelPerfect = false; // 闈炲儚绱犲畬缇庯紝鎻愬崌鎬ц兘

            // 鏍稿績閰嶇疆2锛氭坊鍔燙anvasScaler锛堟帶鍒禪I缂╂斁锛屽繀鍔狅級
            CanvasScaler canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1080, 1920); // 鍙傝€冨垎杈ㄧ巼锛堜富娴佹墜鏈猴級
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f; // 瀹介珮閫傞厤鏉冮噸

            // 鏍稿績閰嶇疆3锛氭坊鍔燝raphicRaycaster锛圲I灏勭嚎妫€娴嬶紝蹇呭姞锛屽惁鍒橴I鏃犳硶鍝嶅簲鐐瑰嚮锛?
            canvasGO.AddComponent<GraphicRaycaster>();

            // 鍙€夛細璁剧疆Canvas涓哄満鏅牴鑺傜偣锛堝眰绾ф洿娓呮櫚锛?
            canvasGO.transform.SetAsFirstSibling();

            Debug.Log("鏂癈anvas鍒涘缓瀹屾垚锛屽凡閰嶇疆榛樿鍙傛暟");
            return newCanvas;
        }
        public bool IsPrefab(GameObject obj)
        {
            // 1. 鍏堝垽绌猴紝閬垮厤绌哄紩鐢ㄦ姤閿?
            if (obj == null)
            {
                Debug.LogWarning("Target GameObject is null, cannot determine whether it is a prefab.");
                return false;
            }

            // 2. 鏍稿績鍒ゆ柇锛氶鍒朵綋涓嶅睘浜庝换浣曟湁鏁堝満鏅?
            // 鍦烘櫙瀹炰緥鍖栧璞＄殑scene.IsValid()杩斿洖true锛岄鍒朵綋杩斿洖false
            if (!obj.scene.IsValid())
            {
                return true;
            }

            // 3. 棰濆鍒ゆ柇锛氬満鏅腑瀵硅薄鏄惁鏄鍒朵綋鐨勫疄渚嬶紙鍙€夛級
            // 鑻ラ渶瑕佸尯鍒嗐€岄鍒朵綋婧愭枃浠躲€嶅拰銆屽満鏅腑鐨勯鍒朵綋瀹炰緥銆嶏紝鍙姞姝ら€昏緫
            return false;
        }
        /// <summary>
        /// 缂栬緫鍣ㄤ笅鍒ゆ柇涓や釜GameObject鏄惁鏉ヨ嚜鍚屼竴涓狿refab
        /// </summary>
        /// <param name="obj1">绗竴涓墿浣?/param>
        /// <param name="obj2">绗簩涓墿浣?/param>
        /// <returns>鏄惁涓哄悓涓€Prefab瀹炰緥</returns>
        public bool IsSamePrefabSource(String script,GameObject obj)
        {
            // 绌哄€煎垽鏂?
            if (script == "" && obj == null)
            {
                Debug.LogError("Input GameObject cannot be null.");
                return false;
            }

            var components = obj.GetComponents<Component>();
            foreach (var c in components)
            {
                if (c != null && c.GetType().Name == script)
                    return true;
            }
            return false;
        }

        /// <summ
    }
}

