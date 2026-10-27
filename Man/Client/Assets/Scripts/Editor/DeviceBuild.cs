using UnityEditor;
using UnityEngine;

/// <summary>
/// iOS 出包入口，供命令行调用：
///   Unity -batchmode -quit -buildTarget iOS -projectPath <Client> -executeMethod DeviceBuild.Build
/// 导出目录由环境变量 TDJ_IOS_OUT 指定。
/// 导出后的签名/部署目标/StoreKit/UIScene 由 IOSBuildPostProcess 自动补齐。
/// </summary>
public class DeviceBuild
{
    // 顺序必须与 GameSceneType 一致：Title=0, Rpg=1, Camp=2, Battle=3
    private static readonly string[] Scenes =
    {
        "Assets/Scenes/TitleScene.unity",
        "Assets/Scenes/RPGScene.unity",
        "Assets/Scenes/CampScene.unity",
        "Assets/Scenes/BattleScene.unity",
    };

    [MenuItem("天地劫/iOS/导出到设备构建目录")]
    public static void Build()
    {
        string outDir = System.Environment.GetEnvironmentVariable("TDJ_IOS_OUT");
        if (string.IsNullOrEmpty(outDir))
            outDir = "/Users/joekeikun/tdj_ios_device_build";

        BuildPlayerOptions opt = new BuildPlayerOptions();
        opt.scenes = Scenes;
        opt.locationPathName = outDir;
        opt.target = BuildTarget.iOS;
        opt.options = BuildOptions.None;
        BuildPipeline.BuildPlayer(opt);
    }
}
