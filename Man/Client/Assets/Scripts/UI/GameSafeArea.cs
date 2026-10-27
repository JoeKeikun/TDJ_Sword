using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// iOS安全区域适配组件
/// 自动调整UI RectTransform 以避开刘海屏、灵动岛、底部Home Indicator等
/// 将此脚本挂载在需要适配安全区域的Canvas或UI根节点上
/// </summary>
public class GameSafeArea : MonoBehaviour
{
    RectTransform panel;
    Rect lastSafeArea = new Rect(0, 0, 0, 0);
    ScreenOrientation lastOrientation = ScreenOrientation.Unknown;

    void Awake()
    {
        panel = GetComponent<RectTransform>();
        if (panel == null)
        {
            Debug.LogWarning("[GameSafeArea] RectTransform not found on " + gameObject.name);
            enabled = false;
            return;
        }
    }

    void Start()
    {
        ApplySafeArea();
    }

    void Update()
    {
        // 检测安全区域或屏幕方向变化
        if (Screen.safeArea != lastSafeArea || Screen.orientation != lastOrientation)
        {
            ApplySafeArea();
        }
    }

    void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;

        // 编辑器或非移动平台直接用全屏
#if UNITY_EDITOR || (!UNITY_IOS && !UNITY_ANDROID)
        // 在编辑器中可以模拟安全区域进行测试
        // safeArea = new Rect(0, 102, 1170, 2340); // iPhone 14 Pro 模拟
#endif

        if (safeArea == lastSafeArea && Screen.orientation == lastOrientation)
            return;

        lastSafeArea = safeArea;
        lastOrientation = Screen.orientation;

        if (panel == null)
            return;

        // 将安全区域转换为锚点偏移
        // 注意：safeArea 是以像素为单位，原点在左下角
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        panel.anchorMin = anchorMin;
        panel.anchorMax = anchorMax;

        // 重置偏移，让锚点决定位置
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;

        Debug.LogFormat("[GameSafeArea] Applied: safeArea={0}x{1} at ({2},{3}), anchors=({4:F2},{5:F2})-({6:F2},{7:F2})",
            safeArea.width, safeArea.height,
            safeArea.x, safeArea.y,
            anchorMin.x, anchorMin.y,
            anchorMax.x, anchorMax.y);
    }
}
