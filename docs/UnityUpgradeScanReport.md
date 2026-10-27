# Unity 升级过时 API 扫描报告

**扫描目录**: `Man/Client/Assets/Scripts`
**扫描日期**: 2026-10-25
**扫描范围**: 所有 .cs 文件（共 14 类过时 API 模式）

> 说明：本文件是升级前的**扫描快照**，其中的问题已全部处理，修复细节与验证结果见 [Unity升级兼容性修复报告](./Unity升级兼容性修复报告.md)。真机运行期新增的修复也记录在该报告中。

---

## 总览

| 类别 | 状态 | 发现数量 | 优先级 |
|------|------|----------|--------|
| UNITY_IPHONE 宏 | 有问题 | 17 处 | 高 |
| UNITY_WEBPLAYER 宏 | 有问题 | 4 处 | 中 |
| UnityWebRequest.isNetworkError/isHttpError | 有问题 | 5 处 | 高 |
| UnityWebRequest.error 属性 | 有问题 | 3 处 | 高 |
| yield return www（旧式写法） | 有问题 | 2 处 | 中 |
| WWW 类 | 未发现 | 0 | - |
| Application.LoadLevel | 未发现 | 0 | - |
| Component 快捷属性 | 未发现 | 0 | - |
| GUIText / GUITexture | 未发现 | 0 | - |
| ParticleSystem 旧 API | 未发现 | 0 | - |
| iPhoneSettings | 未发现 | 0 | - |
| Resources.LoadAssetAtPath | 未发现 | 0 | - |
| [Obsolete] 标记 | 未发现 | 0 | - |
| UnityTest / NUnit | 未发现 | 0 | - |

---

## 一、UNITY_IPHONE 宏（应改为 UNITY_IOS）

**严重程度**: 高
**影响**: Unity 2018.1+ 中 `UNITY_IPHONE` 已被弃用，应使用 `UNITY_IOS`。在较新版本中可能导致编译警告或错误。
**建议修改**: 全局替换 `UNITY_IPHONE` 为 `UNITY_IOS`

### 详细清单

#### 1. Base/GameSetting.cs
- **行 165**: `#elif UNITY_IPHONE`
  ```csharp
  #elif UNITY_IPHONE
      StreamingAssetsPath = Application.dataPath + "/Raw/";
  ```
  **修改为**: `#elif UNITY_IOS`

#### 2. Title/GameSceneTitle.cs
- **行 19**: `#if UNITY_IPHONE`
  ```csharp
  #if UNITY_IPHONE
      onTitleMovieOver();
  #else
      GameMovieManager.instance.playMovieCenter( "Movie/Title" , onTitleMovieOver );
  #endif
  ```
  **修改为**: `#if UNITY_IOS`

#### 3. Data/GameIOSPayData.cs
- **行 7**: `#if UNITY_IPHONE`
  ```csharp
  #if UNITY_IPHONE
  public class GameIOSPayData : SingletonManager< GameIOSPayData > 
  {
      // ...
  }
  #endif
  ```
  **修改为**: `#if UNITY_IOS`

#### 4. Base/GamePHP.cs
- **行 36**: `#if UNITY_IPHONE`
  ```csharp
  #if UNITY_IPHONE
      string urlPay = "https://sword.foxgames.cn/requestIOSPay.php?";
      string url = "https://sword.foxgames.cn/sqlRequestIOS.php?sql=";
      string urlLog = "https://sword.foxgames.cn/sqlRequestLog.php?str=";
      string urlCheck = "https://sword.foxgames.cn/requestIOSCheck.php?";
  ```
  **修改为**: `#if UNITY_IOS`

- **行 57**: `#if UNITY_IPHONE`
  ```csharp
  #if UNITY_IPHONE
      str = " iphone " + str;
  #endif
  ```
  **修改为**: `#if UNITY_IOS`

- **行 67**: `#if UNITY_IPHONE`
  ```csharp
  #if UNITY_IPHONE
      public void phpIOSCheck(string dat, phpCallback cb) { ... }
  ```
  **修改为**: `#if UNITY_IOS`

#### 5. Manager/GameTouchManager.cs
- **行 15**: `#if ( UNITY_ANDROID || UNITY_IPHONE )`
  ```csharp
  #if ( UNITY_ANDROID || UNITY_IPHONE )
      IsShow = true;
  #else
      IsShow = false;
  #endif
  ```
  **修改为**: `#if ( UNITY_ANDROID || UNITY_IOS )`

#### 6. Manager/GameManager.cs
- **行 10**: `#if UNITY_IPHONE && !UNITY_EDITOR`
  ```csharp
  #if UNITY_IPHONE && !UNITY_EDITOR
      [DllImport("__Internal")]
      internal extern static int joinGroup1();
      // ...
  #endif
  ```
  **修改为**: `#if UNITY_IOS && !UNITY_EDITOR`

- **行 91**: `#if UNITY_IPHONE && !UNITY_EDITOR`
  ```csharp
  #if UNITY_IPHONE && !UNITY_EDITOR
      try
      {
          GamePHP.instance.phpIOSCheck(Application.identifier + "_" + getMsg1() + "_" + getMsg2(), null);
      }
      // ...
  #endif
  ```
  **修改为**: `#if UNITY_IOS && !UNITY_EDITOR`

- **行 127**: `#if UNITY_IPHONE && !UNITY_EDITOR`
  ```csharp
  #if UNITY_IPHONE && !UNITY_EDITOR
  //        GameIOSPayData.instance.init();
  #endif
  ```
  **修改为**: `#if UNITY_IOS && !UNITY_EDITOR`

#### 7. UI/GameActiveUI.cs
- **行 13**: `#if UNITY_IPHONE && !UNITY_EDITOR`
  ```csharp
  #if UNITY_IPHONE && !UNITY_EDITOR
  //        GameIOSPayData.instance.IAPBuyGoods( 0 );
  #elif UNITY_ANDROID && !UNITY_EDITOR
  ```
  **修改为**: `#if UNITY_IOS && !UNITY_EDITOR`

#### 8. UI/GameTouchCenterUI.cs
- **行 13**: `#if ( UNITY_ANDROID || UNITY_IPHONE )`
  ```csharp
  #if ( UNITY_ANDROID || UNITY_IPHONE )
      if ( GameTouchLeftUI.instance.IsShow || 
          GameTouchLeftUI.instance.IsFadeIn )
      {
          return;
      }
      // ...
  ```
  **修改为**: `#if ( UNITY_ANDROID || UNITY_IOS )`

- **行 39**: `#if ( UNITY_ANDROID || UNITY_IPHONE )`
  ```csharp
  #if ( UNITY_ANDROID || UNITY_IPHONE )
      if ( !GameTouchLeftUI.instance.IsShow ||
          GameTouchLeftUI.instance.IsFadeOut )
      {
          return;
      }
      // ...
  ```
  **修改为**: `#if ( UNITY_ANDROID || UNITY_IOS )`

#### 9. UI/GameHelpUI.cs
- **行 233**: `#if UNITY_IPHONE && !UNITY_EDITOR`
  ```csharp
  #if UNITY_IPHONE && !UNITY_EDITOR
      GameManager.joinGroup1();
  #elif UNITY_ANDROID && !UNITY_EDITOR
  ```
  **修改为**: `#if UNITY_IOS && !UNITY_EDITOR`

- **行 255**: `#if UNITY_IPHONE`
  ```csharp
  #if UNITY_IPHONE
      const string APP_ID = "1461510167";
      var url = string.Format(
          "itms-apps://itunes.apple.com/cn/app/id{0}?mt=8&action=write-review" ,
          APP_ID );
      Application.OpenURL( url );
  #endif
  ```
  **修改为**: `#if UNITY_IOS`

#### 10. Battle/GameBattleUserRightUI.cs
- **行 42**: `#if ( UNITY_ANDROID || UNITY_IPHONE )`
  ```csharp
  #if ( UNITY_ANDROID || UNITY_IPHONE )
      top = true;
  #endif
  ```
  **修改为**: `#if ( UNITY_ANDROID || UNITY_IOS )`

#### 11. Battle/GameBattleUserLeftUI.cs
- **行 39**: `#if ( UNITY_ANDROID || UNITY_IPHONE )`
  ```csharp
  #if ( UNITY_ANDROID || UNITY_IPHONE )
      top = true;
  #endif
  ```
  **修改为**: `#if ( UNITY_ANDROID || UNITY_IOS )`

---

## 二、UNITY_WEBPLAYER 宏

**严重程度**: 中
**影响**: Unity 5.4 起已移除 Web Player 平台支持。该宏在新版本中永远不会被定义，相关代码成为死代码。
**建议**: 如果不再支持 WebPlayer 平台，可以直接删除相关分支；如需保留 WebGL 支持，保留 `UNITY_WEBGL` 分支即可。

### 详细清单

#### 1. Base/GameSetting.cs

- **行 169**: `#elif UNITY_WEBPLAYER`
  ```csharp
  #elif UNITY_WEBPLAYER
      StreamingAssetsPath = Application.dataPath + "/StreamingAssets/";
  #else
      StreamingAssetsPath = Application.streamingAssetsPath + "/";
  #endif
  ```
  **建议**: 删除此分支，或确认 `#else` 分支（使用 `Application.streamingAssetsPath`）已涵盖 WebGL。

- **行 230**: `#if UNITY_WEBPLAYER || UNITY_WEBGL`
  ```csharp
  #if UNITY_WEBPLAYER || UNITY_WEBGL
      AddonPath = StreamingAssetsPath;
  #else
      AddonPath = PersistentDataPath;
  #endif
  ```
  **建议**: 简化为 `#if UNITY_WEBGL`

- **行 259**: `#if UNITY_WEBPLAYER`
  ```csharp
  #if UNITY_WEBPLAYER
      FileStream stream = File.Open( path , FileMode.Open );
  #else
      FileStream stream = File.Open( path , FileMode.Open , FileAccess.Read );
  #endif
  ```
  **建议**: 删除此分支，直接使用 `FileMode.Open, FileAccess.Read` 版本。

- **行 291**: `#if UNITY_WEBPLAYER || UNITY_WEBGL`
  ```csharp
  #if UNITY_WEBPLAYER || UNITY_WEBGL
      if ( GameManager.instance) 
      {
          GameManager.instance.StartCoroutine( wwwLoad( path , cb ) );
      }
  #else
      FileStream stream = File.Open( path , FileMode.Open , FileAccess.Read );
      // ...
  ```
  **建议**: 简化为 `#if UNITY_WEBGL`

---

## 三、UnityWebRequest.isNetworkError / isHttpError（弃用属性）

**严重程度**: 高
**影响**: Unity 2020.1+ 中 `UnityWebRequest.isNetworkError` 和 `UnityWebRequest.isHttpError` 已标记为 `[Obsolete]`。Unity 2021.2+ 中被移除。
**建议修改**: 使用 `UnityWebRequest.result` 属性替代。

| 旧式写法 | 新式写法 |
|---------|---------|
| `if (www.isNetworkError \|\| www.isHttpError)` | `if (www.result != UnityWebRequest.Result.Success)` |
| `if (www.isNetworkError)` | `if (www.result == UnityWebRequest.Result.ConnectionError)` |
| `if (www.isHttpError)` | `if (www.result == UnityWebRequest.Result.ProtocolError)` |

### 详细清单

#### 1. Base/GamePHP.cs

- **行 82**: `if (www.isNetworkError || www.isHttpError)`
  ```csharp
  UnityWebRequest www = UnityWebRequest.Get(sql);
  yield return www.SendWebRequest();

  if (www.isNetworkError || www.isHttpError)
  {
  }
  else
  {
      int n = (www.downloadHandler.text.Contains("1") ? 1 : 0);
      // ...
  }
  ```
  **修改为**:
  ```csharp
  if (www.result != UnityWebRequest.Result.Success)
  ```

- **行 118**: `if (www.isNetworkError || www.isHttpError)`
  ```csharp
  UnityWebRequest www = UnityWebRequest.Get(sql);
  yield return www.SendWebRequest();

  if (www.isNetworkError || www.isHttpError)
  {
  }
  else
  {
      int n = (www.downloadHandler.text.Contains("1") ? 1 : 0);
      // ...
  }
  ```
  **修改为**:
  ```csharp
  if (www.result != UnityWebRequest.Result.Success)
  ```

- **行 200**: `if ( www.isNetworkError || www.isHttpError )`
  ```csharp
  UnityWebRequest www = UnityWebRequest.Get( sql );
  yield return www.SendWebRequest();

  if ( www.isNetworkError || www.isHttpError )
  {
  #if UNITY_EDITOR
      Debug.Log( www.error );
  #endif
  }
  else
  {
      // ...
  }
  ```
  **修改为**:
  ```csharp
  if (www.result != UnityWebRequest.Result.Success)
  ```

- **行 233**: `if ( www.isNetworkError || www.isHttpError )`
  ```csharp
  if ( www.isNetworkError || www.isHttpError )
  {
  #if UNITY_EDITOR
      Debug.Log( www.error );
  #endif
  }
  else
  {
      byte[] bytes = getByte( www.downloadHandler.text );
      // ...
  }
  ```
  **修改为**:
  ```csharp
  if (www.result != UnityWebRequest.Result.Success)
  ```

- **行 264**: `if ( www.isNetworkError || www.isHttpError )`
  ```csharp
  if ( www.isNetworkError || www.isHttpError )
  {
  #if UNITY_EDITOR
      Debug.Log( www.error );
  #endif
  }
  else
  {
      int n = ( www.downloadHandler.text.Contains( "1" ) ? 1 : 0 );
      // ...
  }
  ```
  **修改为**:
  ```csharp
  if (www.result != UnityWebRequest.Result.Success)
  ```

---

## 四、UnityWebRequest.error 属性（弃用）

**严重程度**: 高
**影响**: Unity 2020.1+ 中 `UnityWebRequest.error` 也已标记为 `[Obsolete]`。应通过 `result` 属性判断错误类型。
**建议**: 配合 `result` 属性使用，或在确认错误后通过 `www.error` 获取错误信息（仍可在错误分支中使用以获取描述文本，但建议迁移）。

### 详细清单

#### 1. Base/GamePHP.cs

- **行 203**: `Debug.Log( www.error );`
- **行 236**: `Debug.Log( www.error );`
- **行 267**: `Debug.Log( www.error );`

**说明**: 这三处在 `isNetworkError/isHttpError` 分支内，用于日志输出。`error` 属性虽已弃用但仍可获取错误文本。如果仅需日志，可暂时保留，但建议与 `result` 配合使用。

#### 2. Base/GameSetting.cs

- **行 352**: `if ( www.error != null )`
  ```csharp
  UnityWebRequest www = new UnityWebRequest( url );
  yield return www;

  if ( www.isDone )
  {
      if ( www.error != null ) 
      {
          Debug.LogWarning( url + " load error." );
      }
      // ...
  }
  ```

- **行 398**: `if ( www.error != null )`
  ```csharp
  UnityWebRequest www = new UnityWebRequest( url );
  yield return www;

  if ( www.isDone )
  {
      if ( www.error != null ) 
      {
          // 空处理
      }
      // ...
  }
  ```

**建议修改**: 使用 `www.result != UnityWebRequest.Result.Success` 替代 `www.error != null` 判断。

---

## 五、yield return www（旧式 UnityWebRequest 写法）

**严重程度**: 中
**影响**: 在新版 Unity 中，直接 `yield return www;` 不会自动发送请求，必须显式调用 `www.SendWebRequest()` 并 yield 其返回值。
**建议修改**: 将 `yield return www;` 改为 `yield return www.SendWebRequest();`

### 详细清单

#### 1. Base/GameSetting.cs

- **行 347-348**:
  ```csharp
  UnityWebRequest www = new UnityWebRequest( url );
  yield return www;
  ```
  **修改为**:
  ```csharp
  UnityWebRequest www = UnityWebRequest.Get(url);
  yield return www.SendWebRequest();
  ```

- **行 393-394**:
  ```csharp
  UnityWebRequest www = new UnityWebRequest( url );
  yield return www;
  ```
  **修改为**:
  ```csharp
  UnityWebRequest www = UnityWebRequest.Get(url);
  yield return www.SendWebRequest();
  ```

**补充说明**: 这两处还使用了 `new UnityWebRequest(url)` 构造函数且未设置 downloadHandler，在新版 Unity 中 `www.downloadHandler.data` 可能为 null。建议使用 `UnityWebRequest.Get(url)` 静态方法，它会自动配置 DownloadHandler。

---

## 六、其他发现与说明

### 6.1 UnityEngine.Networking 引用（正常）

**文件**: 
- `Base/GamePHP.cs` 第 7 行
- `Base/GameSetting.cs` 第 9 行

**说明**: 项目已从 WWW 类迁移到 UnityWebRequest（使用 `UnityEngine.Networking`），这是正确的方向。但部分 API 用法仍需更新（见第三、四、五节）。

### 6.2 PlayerPrefs 使用（正常）

共发现 29 处 `PlayerPrefs` 使用，API 无变化，无需修改。

### 6.3 SceneManager 使用（正常）

`Manager/GameSceneManager.cs` 中已使用 `SceneManager.LoadSceneAsync`，说明场景加载 API 已完成迁移，无需修改。

### 6.4 OnGUI 系统（低优先级）

`Manager/GameCameraManager.cs` 第 257 行存在 `OnGUI()` 方法，但内部代码全部为注释状态。Unity 旧 IMGUI 系统虽未移除，但在 UGUI 普及的项目中通常无需使用。此代码不影响运行。

### 6.5 RuntimePlatform.WSAPlayerXxx（注意）

`Base/GameSetting.cs` 第 224-226 行使用了 `RuntimePlatform.WSAPlayerARM`、`WSAPlayerX64`、`WSAPlayerX86`。这些 UWP 平台枚举在较新 Unity 版本中可能被重命名或整合。如不涉及 UWP 发布可忽略。

### 6.6 Resources.Load 使用（注意，非废弃）

项目中有大量 `Resources.Load` 调用（100+ 处）。虽然 `Resources.Load` 本身并未废弃，但大量使用 Resources 文件夹会导致包体增大、内存管理困难。长期建议考虑 Addressables 或 AssetBundle 方案，但这不属于升级必须修改项。

### 6.7 Editor 代码在运行时程序集中（已正确包裹）

`Game/GameAnimation.cs` 第 476 行使用了 `UnityEditor.AssetDatabase.LoadAllAssetsAtPath`，但该代码位于 `#if UNITY_EDITOR` 块（第 393-500 行）内，不会影响运行时构建，无需修改。

---

## 七、未发现问题的 API 列表

以下模式在扫描范围内**未发现**任何使用，说明代码质量较好：

1. **WWW 类**: `new WWW()`、`WWW.LoadFromCacheOrDownload` 等 - 已全部迁移到 UnityWebRequest
2. **Application.LoadLevel / LoadLevelAdditive**: 已迁移到 SceneManager
3. **Component 快捷属性**: `.renderer`、`.collider`、`.rigidbody`、`.audio`、`.light`、`.camera`、`.guiText`、`.guiTexture` - 均未使用
4. **GUIText / GUITexture**: 未使用（项目使用 UGUI）
5. **ParticleSystem 旧 API**: `particleSystem.emission` 等直接访问子模块的方式 - 未发现
6. **iPhoneSettings API**: 未使用
7. **Resources.LoadAssetAtPath**: 未使用（Editor 中使用的是 `AssetDatabase.LoadAssetAtPath`，正确）
8. **[Obsolete] 标记**: 代码中未定义任何标记为 Obsolete 的 API
9. **UnityTest / NUnit 框架**: 项目中无测试代码

---

## 八、升级建议优先级

### 必须修改（升级到 Unity 2020+ 会编译失败或警告）

1. **UNITY_IPHONE → UNITY_IOS**（17 处）- 全局替换即可
2. **UnityWebRequest.isNetworkError/isHttpError**（5 处）- 改为 `result != Success`
3. **UnityWebRequest.error 判断**（2 处，GameSetting.cs）- 配合 result 使用
4. **yield return www → yield return www.SendWebRequest()**（2 处）- 否则请求不会发送

### 建议清理

5. **UNITY_WEBPLAYER 宏**（4 处）- 删除死代码，简化逻辑
6. **new UnityWebRequest(url) → UnityWebRequest.Get(url)**（2 处）- 确保 downloadHandler 正确配置

### 长期优化（非必须）

7. Resources.Load 大量使用 → 考虑 Addressables
8. OnGUI 调试代码 → 清理注释代码

---

*报告结束*
