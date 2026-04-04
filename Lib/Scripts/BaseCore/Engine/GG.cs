
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
namespace SP
{
    
    public class gg : SingletonMono<gg>
    {
        /// <summary>
        /// 检查并创建 EventSystem（核心方法）
        /// </summary>
        public void CreateEventSystemIfNotExists()
        {
            // 1. 查找场景中是否已有 EventSystem
            EventSystem existingEventSystem = FindObjectOfType<EventSystem>();
        
            // 2. 如果没有，则创建
            if (existingEventSystem == null)
            {
                // 创建空 GameObject 并命名
                GameObject eventSystemGO = new GameObject("EventSystem");
            
                // 3. 添加核心的 EventSystem 组件
                EventSystem eventSystem = eventSystemGO.AddComponent<EventSystem>();
            
                // 4. 添加输入模块（必须！否则 EventSystem 无法处理任何输入事件）
                StandaloneInputModule inputModule = eventSystemGO.AddComponent<StandaloneInputModule>();
            
                // 可选：设置输入模块的参数（默认值即可满足大部分需求）
                inputModule.horizontalAxis = "Horizontal";
                inputModule.verticalAxis = "Vertical";
                inputModule.submitButton = "Submit";
                inputModule.cancelButton = "Cancel";

                Debug.Log("EventSystem 已通过代码创建完成");
            }
            else
            {
                Debug.Log("场景中已存在 EventSystem，无需重复创建");
            }
        }

        /// <summary>
        /// 获取场景中的Canvas，若不存在则创建并返回
        /// </summary>
        /// <returns>场景中的Canvas组件（确保非null）</returns>
        public Canvas GetOrCreateCanvas()
        {
            // 1. 查找场景中已有的Canvas（优先找启用的）
            Canvas existingCanvas = FindObjectOfType<Canvas>();

            // 2. 如果找到，直接返回
            if (existingCanvas != null)
            {
                return existingCanvas;
            }

            // 3. 未找到则创建新Canvas
            Debug.Log("未找到Canvas，开始创建新Canvas");

            // 创建Canvas GameObject
            GameObject canvasGO = new GameObject("Canvas");
            Canvas newCanvas = canvasGO.AddComponent<Canvas>();

            // 核心配置1：设置为屏幕空间-叠加（最常用的UI渲染模式）
            newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            newCanvas.pixelPerfect = false; // 非像素完美，提升性能

            // 核心配置2：添加CanvasScaler（控制UI缩放，必加）
            CanvasScaler canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1080, 1920); // 参考分辨率（主流手机）
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f; // 宽高适配权重

            // 核心配置3：添加GraphicRaycaster（UI射线检测，必加，否则UI无法响应点击）
            canvasGO.AddComponent<GraphicRaycaster>();

            // 可选：设置Canvas为场景根节点（层级更清晰）
            canvasGO.transform.SetAsFirstSibling();

            Debug.Log("新Canvas创建完成，已配置默认参数");
            return newCanvas;
        }
        public bool IsPrefab(GameObject obj)
        {
            // 1. 先判空，避免空引用报错
            if (obj == null)
            {
                Debug.LogWarning("目标GameObject为空，无法判断是否为预制体");
                return false;
            }

            // 2. 核心判断：预制体不属于任何有效场景
            // 场景实例化对象的scene.IsValid()返回true，预制体返回false
            if (!obj.scene.IsValid())
            {
                return true;
            }

            // 3. 额外判断：场景中对象是否是预制体的实例（可选）
            // 若需要区分「预制体源文件」和「场景中的预制体实例」，可加此逻辑
            return false;
        }
        /// <summary>
        /// 编辑器下判断两个GameObject是否来自同一个Prefab
        /// </summary>
        /// <param name="obj1">第一个物体</param>
        /// <param name="obj2">第二个物体</param>
        /// <returns>是否为同一Prefab实例</returns>
        public bool IsSamePrefabSource(String script,GameObject obj)
        {
            // 空值判断
            if (script == "" && obj == null)
            {
                Debug.LogError("传入的GameObject不能为空！");
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
