using UnityEngine;
using TMPro; // 【改动1】引入 TextMeshPro 的命名空间，替换掉原来的 UnityEngine.UI

public class FPSDisplayUI : MonoBehaviour
{
    [Tooltip("拖拽你的 UI Text 到这里")]
    public TMP_Text fpsText; // 【改动2】把 Text 改成 TMP_Text
    
    private float deltaTime = 0.0f;

    void Awake()
    {
        // 保证这个帧率组件跨场景不被销毁
        // DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (fpsText == null) return;

        // 计算平滑的 deltaTime
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        
        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        
        fpsText.text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);

        // 根据帧率自动变色
        if (fps < 30)
            fpsText.color = Color.red;
        else if (fps < 55)
            fpsText.color = Color.yellow;
        else
            fpsText.color = Color.green;
    }
}