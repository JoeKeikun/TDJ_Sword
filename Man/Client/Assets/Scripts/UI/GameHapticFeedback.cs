using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// iOS触觉反馈管理器
/// 提供轻量、中等、重度三种震动反馈
/// 仅在iOS真机上生效，其他平台自动忽略
/// </summary>
public static class GameHapticFeedback
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void _hapticImpactLight();
    
    [DllImport("__Internal")]
    private static extern void _hapticImpactMedium();
    
    [DllImport("__Internal")]
    private static extern void _hapticImpactHeavy();
    
    [DllImport("__Internal")]
    private static extern void _hapticSelectionChanged();
    
    [DllImport("__Internal")]
    private static extern void _hapticNotificationSuccess();
    
    [DllImport("__Internal")]
    private static extern void _hapticNotificationWarning();
    
    [DllImport("__Internal")]
    private static extern void _hapticNotificationError();
#endif

    /// <summary>
    /// 震动强度
    /// </summary>
    public enum HapticType
    {
        Light = 0,      // 轻量 - 轻微点击感
        Medium = 1,     // 中等 - 普通按钮点击
        Heavy = 2,      // 重度 - 重要操作确认
        Selection = 3,  // 选择反馈 - 滚动选择
        Success = 4,    // 成功通知
        Warning = 5,    // 警告通知
        Error = 6       // 错误通知
    }

    /// <summary>
    /// 全局开关（可在设置中控制）
    /// </summary>
    public static bool Enabled = true;

    /// <summary>
    /// 触发触觉反馈
    /// </summary>
    public static void Play(HapticType type = HapticType.Medium)
    {
        if (!Enabled)
            return;

#if UNITY_IOS && !UNITY_EDITOR
        switch (type)
        {
            case HapticType.Light:
                _hapticImpactLight();
                break;
            case HapticType.Medium:
                _hapticImpactMedium();
                break;
            case HapticType.Heavy:
                _hapticImpactHeavy();
                break;
            case HapticType.Selection:
                _hapticSelectionChanged();
                break;
            case HapticType.Success:
                _hapticNotificationSuccess();
                break;
            case HapticType.Warning:
                _hapticNotificationWarning();
                break;
            case HapticType.Error:
                _hapticNotificationError();
                break;
        }
#elif UNITY_ANDROID && !UNITY_EDITOR
        // Android震动 - 简单版
        if (type == HapticType.Light)
            Handheld.Vibrate();
        else if (type == HapticType.Medium || type == HapticType.Heavy)
            Handheld.Vibrate();
#else
        // 编辑器/PC平台：不做任何事
#endif
    }
}
