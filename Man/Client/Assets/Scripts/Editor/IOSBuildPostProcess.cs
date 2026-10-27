using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;

/// <summary>
/// iOS 导出后的工程修补。
/// Unity 每次导出都会重新生成 Xcode 工程，以下手工设置会被覆盖，必须在这里补回：
/// 1) iOS 27 SDK 强制 UIScene 生命周期，未适配会在启动时被系统 SIGTRAP 终止
/// 2) Xcode 27 不再支持 deployment target 12.0
/// 3) 签名团队 / 自动签名
/// 4) InAppPurchaseManager 需要的 StoreKit.framework
/// </summary>
public class IOSBuildPostProcess : IPostprocessBuildWithReport
{
    private const string TEAM_ID = "6G372K89C9";
    private const string DEPLOY_TARGET = "15.0";
    private const string DISPLAY_NAME = "天地劫 壹";
    private const string SCENE_MARKER = "// === TDJ iOS UIScene lifecycle patch ===";

    public int callbackOrder { get { return 0; } }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.iOS)
            return;

        string dir = report.summary.outputPath;
        PatchXcodeProject(dir);
        PatchInfoPlist(dir);
        PatchAppController(dir);
    }

    private static void PatchXcodeProject(string dir)
    {
        string path = PBXProject.GetPBXProjectPath(dir);
        PBXProject proj = new PBXProject();
        proj.ReadFromFile(path);

        string main = proj.GetUnityMainTargetGuid();
        string fw = proj.GetUnityFrameworkTargetGuid();

        foreach (string target in new[] { main, fw })
            proj.SetBuildProperty(target, "CODE_SIGN_STYLE", "Automatic");

        proj.AddFrameworkToProject(fw, "StoreKit.framework", false);
        proj.WriteToFile(path);

        // 这些属性 Unity 已写入单值，用 PBXProject 的 Add 会变成数组，只能按文本替换
        string text = File.ReadAllText(path);
        text = text.Replace("IPHONEOS_DEPLOYMENT_TARGET = 12.0;", "IPHONEOS_DEPLOYMENT_TARGET = " + DEPLOY_TARGET + ";");
        text = text.Replace("DEVELOPMENT_TEAM = \"\";", "DEVELOPMENT_TEAM = " + TEAM_ID + ";");
        text = text.Replace("ProvisioningStyle = Manual;", "ProvisioningStyle = Automatic;");
        text = RemoveLinesContaining(text, "PROVISIONING_PROFILE");
        File.WriteAllText(path, text);
    }

    private static string RemoveLinesContaining(string text, string token)
    {
        string[] lines = text.Split('\n');
        using (StringWriter writer = new StringWriter())
        {
            foreach (string line in lines)
            {
                if (line.Contains(token) && line.TrimEnd().EndsWith(";"))
                    continue;
                writer.WriteLine(line.TrimEnd('\r'));
            }
            return writer.ToString();
        }
    }

    private static void PatchInfoPlist(string dir)
    {
        string path = Path.Combine(dir, "Info.plist");
        PlistDocument plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(path));

        if (plist.root.values.ContainsKey("UIApplicationSceneManifest") == false)
        {
            PlistElementDict manifest = plist.root.CreateDict("UIApplicationSceneManifest");
            manifest.SetBoolean("UIApplicationSupportsMultipleScenes", false);
            // 不写 UISceneConfigurations：由 UnityAppController 的 configurationForConnectingSceneSession 提供配置
        }

        plist.root.SetString("CFBundleDisplayName", DISPLAY_NAME);
        File.WriteAllText(path, plist.WriteToString());
    }

    private static void PatchAppController(string dir)
    {
        string path = Path.Combine(dir, "Classes", "UnityAppController.mm");
        string text = File.ReadAllText(path);
        if (text.Contains(SCENE_MARKER))
            return;

        string block = @"
" + SCENE_MARKER + @"
// iOS 27 起必须走 UIScene 生命周期。这里把 scene 回调转发给 UnityAppController，
// 并把 Unity 在 didFinishLaunching 阶段创建的 window 挂到窗口场景上。
#if !PLATFORM_VISIONOS

@interface UnitySceneDelegate : UIResponder <UIWindowSceneDelegate>
@end

@implementation UnitySceneDelegate

- (void)scene:(UIScene*)scene willConnectToSession:(UISceneSession*)session options:(UISceneConnectionOptions*)connectionOptions
{
    if (![scene isKindOfClass: [UIWindowScene class]])
        return;

    id<UIApplicationDelegate> appDelegate = [UIApplication sharedApplication].delegate;
    if ([appDelegate isKindOfClass: [UnityAppController class]])
    {
        UIWindow* window = ((UnityAppController*)appDelegate).window;
        if (window != nil && window.windowScene == nil)
        {
            window.windowScene = (UIWindowScene*)scene;
            [window makeKeyAndVisible];
        }
    }
}

- (void)sceneDidBecomeActive:(UIScene*)scene
{
    id<UIApplicationDelegate> appDelegate = [UIApplication sharedApplication].delegate;
    if ([appDelegate respondsToSelector: @selector(applicationDidBecomeActive:)])
        [appDelegate applicationDidBecomeActive: [UIApplication sharedApplication]];
}

- (void)sceneWillResignActive:(UIScene*)scene
{
    id<UIApplicationDelegate> appDelegate = [UIApplication sharedApplication].delegate;
    if ([appDelegate respondsToSelector: @selector(applicationWillResignActive:)])
        [appDelegate applicationWillResignActive: [UIApplication sharedApplication]];
}

- (void)sceneWillEnterForeground:(UIScene*)scene
{
    id<UIApplicationDelegate> appDelegate = [UIApplication sharedApplication].delegate;
    if ([appDelegate respondsToSelector: @selector(applicationWillEnterForeground:)])
        [appDelegate applicationWillEnterForeground: [UIApplication sharedApplication]];
}

- (void)sceneDidEnterBackground:(UIScene*)scene
{
    id<UIApplicationDelegate> appDelegate = [UIApplication sharedApplication].delegate;
    if ([appDelegate respondsToSelector: @selector(applicationDidEnterBackground:)])
        [appDelegate applicationDidEnterBackground: [UIApplication sharedApplication]];
}

@end

@interface UnityAppController (TDJSceneLifecycle)
@end

@implementation UnityAppController (TDJSceneLifecycle)

- (UISceneConfiguration*)application:(UIApplication*)application configurationForConnectingSceneSession:(UISceneSession*)session options:(UISceneConnectionOptions*)options
{
    UISceneConfiguration* config = [[UISceneConfiguration alloc] initWithName: @""Default Configuration"" sessionRole: session.role];
    config.delegateClass = [UnitySceneDelegate class];
    return config;
}

@end

#endif
";
        File.AppendAllText(path, block);
    }
}
