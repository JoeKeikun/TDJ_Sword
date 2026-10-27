using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// iOS构建工具 - 个人开发版配置
/// 提供菜单项快速配置iOS构建环境
/// </summary>
public class IOSBuildTools
{
    private const string IOS_PERSONAL_DEV_DEFINE = "IOS_PERSONAL_DEV";

    /// <summary>
    /// 切换个人开发模式（添加/移除 IOS_PERSONAL_DEV 编译符号）
    /// </summary>
    [MenuItem("天地劫/iOS/个人开发模式 - 开启")]
    public static void EnablePersonalDevMode()
    {
        SetPersonalDevDefine(true);
        Debug.Log("[iOS Build Tools] 个人开发模式已开启");
    }

    [MenuItem("天地劫/iOS/个人开发模式 - 关闭")]
    public static void DisablePersonalDevMode()
    {
        SetPersonalDevDefine(false);
        Debug.Log("[iOS Build Tools] 个人开发模式已关闭");
    }

    [MenuItem("天地劫/iOS/个人开发模式 - 开启", true)]
    private static bool ValidateEnablePersonalDevMode()
    {
        return !HasPersonalDevDefine();
    }

    [MenuItem("天地劫/iOS/个人开发模式 - 关闭", true)]
    private static bool ValidateDisablePersonalDevMode()
    {
        return HasPersonalDevDefine();
    }

    /// <summary>
    /// 一键配置iOS构建设置（推荐个人开发使用）
    /// </summary>
    [MenuItem("天地劫/iOS/一键配置iOS构建")]
    public static void ConfigureIOSBuild()
    {
        // 1. 开启个人开发模式
        SetPersonalDevDefine(true);
        Debug.Log("[iOS Build Tools] 1. 个人开发模式已开启");

        // 2. 设置包名
        PlayerSettings.applicationIdentifier = "com.foxgame.sword.dev";
        Debug.Log("[iOS Build Tools] 2. Bundle ID 设置为: " + PlayerSettings.applicationIdentifier);

        // 3. 设置产品名和公司名
        PlayerSettings.productName = "天地劫";
        PlayerSettings.companyName = "PersonalDev";
        Debug.Log("[iOS Build Tools] 3. 产品名设置为: 天地劫");

        // 4. 设置屏幕方向（横屏）
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        Debug.Log("[iOS Build Tools] 4. 屏幕方向设置为横屏");

        // 5. 设置目标iOS版本
        PlayerSettings.iOS.targetOSVersionString = "12.0";
        Debug.Log("[iOS Build Tools] 5. 最低iOS版本: 12.0");

        // 6. 设置架构为ARM64
        PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, 1); // 1 = ARM64
        Debug.Log("[iOS Build Tools] 6. 架构: ARM64");

        // 7. 设置渲染API为Metal
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, true);
        Debug.Log("[iOS Build Tools] 7. 渲染API: Metal (默认)");

        // 8. 关闭HTTP下载限制
        PlayerSettings.iOS.allowHTTPDownload = true;
        Debug.Log("[iOS Build Tools] 8. HTTP下载: 开启");

        // 9. 设置状态栏行为
#if UNITY_2022_2_OR_NEWER
        PlayerSettings.iOS.statusBarStyle = iOSStatusBarStyle.Default;
#else
        PlayerSettings.iOS.statusBarHidden = true;
#endif
        Debug.Log("[iOS Build Tools] 9. 状态栏: 隐藏");

        // 10. 设置帧率
        Application.targetFrameRate = 60;
        Debug.Log("[iOS Build Tools] 10. 目标帧率: 60");

        // 11. 确保场景正确添加
        CheckBuildScenes();

        Debug.Log("[iOS Build Tools] ===== iOS构建配置完成 =====");
        EditorUtility.DisplayDialog("iOS构建配置", "iOS个人开发版配置已完成！\n\n请检查 File → Build Settings → Player Settings\n确保签名和Bundle ID正确。", "好的");
    }

    /// <summary>
    /// 导出iOS Xcode项目
    /// </summary>
    [MenuItem("天地劫/iOS/导出Xcode项目")]
    public static void ExportIOSProject()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
        {
            EditorUtility.DisplayDialog("提示", "请先切换到iOS平台\n(File → Build Settings → iOS → Switch Platform)", "好的");
            return;
        }

        string outputPath = EditorUtility.SaveFolderPanel("选择Xcode项目输出目录", "", "iOSBuild");
        if (string.IsNullOrEmpty(outputPath))
        {
            return;
        }

        // 确保场景都在构建设置中
        string[] scenes = GetBuildScenes();
        if (scenes == null || scenes.Length == 0)
        {
            EditorUtility.DisplayDialog("错误", "没有找到构建场景！\n请检查 File → Build Settings 中的场景列表", "好的");
            return;
        }

        Debug.Log("[iOS Build Tools] 开始导出Xcode项目到: " + outputPath);

        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = scenes;
        options.locationPathName = outputPath;
        options.target = BuildTarget.iOS;
        options.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("[iOS Build Tools] Xcode项目导出成功！");
            EditorUtility.DisplayDialog("导出成功", "Xcode项目已导出到:\n" + outputPath + "\n\n请用Xcode打开并配置签名后构建。", "好的");
        }
        else
        {
            Debug.LogError("[iOS Build Tools] Xcode项目导出失败！");
            EditorUtility.DisplayDialog("导出失败", "构建失败，请查看Console日志。", "好的");
        }
    }

    /// <summary>
    /// 检查并打印当前iOS配置状态
    /// </summary>
    [MenuItem("天地劫/iOS/查看当前iOS配置")]
    public static void CheckIOSConfig()
    {
        string status = HasPersonalDevDefine() ? "已开启" : "已关闭";

        string config = "===== 当前iOS构建配置 =====\n\n" +
            "个人开发模式: " + status + "\n" +
            "编译符号: " + GetDefines() + "\n\n" +
            "Bundle ID: " + PlayerSettings.applicationIdentifier + "\n" +
            "产品名: " + PlayerSettings.productName + "\n" +
            "公司名: " + PlayerSettings.companyName + "\n\n" +
            "目标iOS版本: " + PlayerSettings.iOS.targetOSVersionString + "\n" +
            "架构: " + (PlayerSettings.GetArchitecture(BuildTargetGroup.iOS) == 1 ? "ARM64" : "Unknown") + "\n\n" +
            "屏幕方向: " + PlayerSettings.defaultInterfaceOrientation + "\n" +
            "目标帧率: " + Application.targetFrameRate + "\n\n" +
            "构建场景数: " + EditorBuildSettings.scenes.Length + "\n";

        for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
        {
            var scene = EditorBuildSettings.scenes[i];
            config += "  [" + i + "] " + (scene.enabled ? "✓" : "✗") + " " + Path.GetFileName(scene.path) + "\n";
        }

        Debug.Log(config);
        EditorUtility.DisplayDialog("iOS配置状态", config, "好的");
    }

    // iOS/Metal 不支持 ETC / DXT，必须使用 ASTC。
    // Unity 2020.1 起枚举名去掉了 RGB/RGBA 前缀，这里做版本兼容。
#if UNITY_2020_1_OR_NEWER
    private const TextureImporterFormat IOSFormatRGBA = TextureImporterFormat.ASTC_6x6;
    private const TextureImporterFormat IOSFormatRGB = TextureImporterFormat.ASTC_6x6;
#else
    private const TextureImporterFormat IOSFormatRGBA = TextureImporterFormat.ASTC_RGBA_6x6;
    private const TextureImporterFormat IOSFormatRGB = TextureImporterFormat.ASTC_RGB_6x6;
#endif

    /// <summary>
    /// 批量为选中贴图设置 iOS 平台的 ASTC 压缩。
    /// 可选中单张贴图，也可选中整个文件夹（递归生效）。
    /// </summary>
    [MenuItem("天地劫/iOS/贴图压缩改为 ASTC (选中贴图/文件夹)")]
    public static void SetSelectedTexturesASTC()
    {
        UnityEngine.Object[] textures = Selection.GetFiltered(typeof(Texture2D), SelectionMode.DeepAssets);
        if (textures.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Project 窗口选中贴图或文件夹。", "好的");
            return;
        }

        int changed = 0;
        foreach (UnityEngine.Object obj in textures)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null)
            {
                continue;
            }

            bool hasAlpha = ti.DoesSourceTextureHaveAlpha();

            TextureImporterPlatformSettings settings = new TextureImporterPlatformSettings();
            settings.overridden = true;
            settings.name = "iPhone";
            settings.format = hasAlpha ? IOSFormatRGBA : IOSFormatRGB;
            settings.maxTextureSize = 4096;
            settings.compressionQuality = 100;
            // ASTC 不支持 crunched 压缩，必须关闭
            settings.crunchedCompression = false;
            settings.textureCompression = TextureImporterCompression.Compressed;

            ti.SetPlatformTextureSettings(settings);
            ti.SaveAndReimport();
            changed++;
        }

        Debug.Log("[iOS Build Tools] 已将 " + changed + " 张贴图的 iPhone 平台格式设为 ASTC。");
        EditorUtility.DisplayDialog("完成", "已处理 " + changed + " 张贴图。", "好的");
    }

    /// <summary>
    /// 检查 SAF 转换所需目录是否存在，并报告缺失项。
    /// </summary>
    [MenuItem("天地劫/iOS/检查 SAF 资源目录")]
    public static void CheckSafResourceDirs()
    {
        string assetsRoot = Application.dataPath;

        string[] requiredDirs = new string[]
        {
            "Objects/SAF",              // SAF 动画源文件
            "Objects/Texture",          // 动画贴图（Sprite Sheet）
            "Objects/DAT/Misc",         // 全局数据表
            "Objects/DAT/Stage",        // 关卡战斗数据
            "Objects/DAT/Rpg",          // RPG 地图数据
            "Objects/DAT/Camp",         // 营地脚本数据
            "Resources/Prefab",         // 转换后的动画预制体
            "Resources/Texture",        // 运行时贴图
            "Resources/Music",
            "Resources/Sound",
            "Resources/Movie"
        };

        string report = "===== SAF 资源目录检查 =====\n\n";
        int missing = 0;

        foreach (string dir in requiredDirs)
        {
            string full = Path.Combine(assetsRoot, dir);
            bool exists = Directory.Exists(full);
            report += (exists ? "✓ " : "✗ ") + "Assets/" + dir + "\n";
            if (!exists)
            {
                missing++;
            }
        }

        report += "\n缺失目录数: " + missing + " / " + requiredDirs.Length;

        Debug.Log(report);
        EditorUtility.DisplayDialog("SAF 资源检查", report, "好的");
    }

    #region 辅助方法

    private static bool HasPersonalDevDefine()
    {
        string defines = GetDefines();
        return defines.Contains(IOS_PERSONAL_DEV_DEFINE);
    }

    private static string GetDefines()
    {
        BuildTargetGroup target = EditorUserBuildSettings.selectedBuildTargetGroup;
        return PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
    }

    private static void SetPersonalDevDefine(bool enable)
    {
        BuildTargetGroup target = EditorUserBuildSettings.selectedBuildTargetGroup;
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);

        if (enable)
        {
            if (!defines.Contains(IOS_PERSONAL_DEV_DEFINE))
            {
                if (defines.Length > 0 && !defines.EndsWith(";"))
                    defines += ";";
                defines += IOS_PERSONAL_DEV_DEFINE;
            }
        }
        else
        {
            defines = defines.Replace(IOS_PERSONAL_DEV_DEFINE, "");
            defines = defines.Replace(";;", ";");
            defines = defines.TrimStart(';').TrimEnd(';');
        }

        PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
    }

    private static void CheckBuildScenes()
    {
        // 检查必要场景是否已添加
        string[] requiredScenes = new string[]
        {
            "Assets/Scenes/TitleScene.unity",
            "Assets/Scenes/RPGScene.unity",
            "Assets/Scenes/CampScene.unity",
            "Assets/Scenes/BattleScene.unity"
        };

        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool changed = false;

        foreach (string scenePath in requiredScenes)
        {
            bool found = false;
            foreach (var s in scenes)
            {
                if (s.path == scenePath)
                {
                    found = true;
                    s.enabled = true;
                    break;
                }
            }

            if (!found && File.Exists(Path.Combine(Application.dataPath, "..", scenePath)))
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                changed = true;
                Debug.Log("[iOS Build Tools] 添加构建场景: " + scenePath);
            }
        }

        if (changed)
        {
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }

    private static string[] GetBuildScenes()
    {
        List<string> scenePaths = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenePaths.Add(scene.path);
            }
        }
        return scenePaths.ToArray();
    }

    #endregion
}
