# Unity 版本升级兼容性修复报告

## 项目信息
- **项目名称**: Sword (战棋 RPG)
- **原始版本**: Unity 2018.3.0f2
- **目标版本**: Unity 2020 LTS / 2022 LTS (推荐)
- **修复日期**: 2026-10-25

---

## 已完成的修复

### 1. 平台宏定义更新 (11 个文件, 17 处)

**问题**: `UNITY_IPHONE` 在 Unity 2018+ 已更名为 `UNITY_IOS`，旧宏虽然仍可用但已不推荐。

**修复**: 全局替换 `UNITY_IPHONE` → `UNITY_IOS`

**涉及文件**:
- `Assets/Scripts/Base/GamePHP.cs`
- `Assets/Scripts/Base/GameSetting.cs`
- `Assets/Scripts/Data/GameIOSPayData.cs`
- `Assets/Scripts/Manager/GameManager.cs`
- `Assets/Scripts/Manager/GameTouchManager.cs`
- `Assets/Scripts/UI/GameActiveUI.cs`
- `Assets/Scripts/UI/GameHelpUI.cs`
- `Assets/Scripts/UI/GameTouchCenterUI.cs`
- `Assets/Scripts/Title/GameSceneTitle.cs`
- `Assets/Scripts/Battle/GameBattleUserLeftUI.cs`
- `Assets/Scripts/Battle/GameBattleUserRightUI.cs`

### 2. UnityWebRequest 废弃 API 修复 (GamePHP.cs, 5 处)

**问题**: `isNetworkError` 和 `isHttpError` 属性在 Unity 2020.1 中已废弃，Unity 2021+ 会编译警告，未来版本将移除。

**修复**: 替换为 `www.result != UnityWebRequest.Result.Success`

**涉及方法**:
- `phpIOSCheck1()`
- `phpIOSPay1()`
- `phpPost()`
- `phpGet()`
- `phpActive()`

### 3. GameSetting.cs 旧式 UnityWebRequest 用法修复 (2 处)

**问题**: 使用 `new UnityWebRequest(url)` + `yield return www` 的旧式写法，没有调用 `SendWebRequest()`，在新版 Unity 中无法正常工作。同时使用了已废弃的 `www.isDone` 和 `www.error` 检查模式。

**修复**:
- `new UnityWebRequest(url)` → `UnityWebRequest.Get(url)`
- `yield return www` → `yield return www.SendWebRequest()`
- `www.isDone` + `www.error` → `www.result != UnityWebRequest.Result.Success`
- 错误回调现在正确调用 `cb(null, true)`（之前逻辑有问题）

**涉及方法**:
- `wwwLoad()`
- `wwwLoadW()`

### 4. UNITY_WEBPLAYER 宏清理 (GameSetting.cs, 4 处)

**问题**: Web Player 平台在 Unity 5.4 中已完全移除，`UNITY_WEBPLAYER` 宏永远不会被定义。

**修复**: 移除所有 `UNITY_WEBPLAYER` 相关分支，保留 `UNITY_WEBGL` 作为 Web 平台替代。

**涉及位置**:
- `initGameSetting()` 中的 StreamingAssetsPath 分支
- `initGameSetting()` 中的 AddonPath 分支
- `loadRes()` 中的 FileStream 分支
- `loadResF()` 中的加载分支

### 5. iOS 内购插件 StoreKit 适配 (InAppPurchaseManager.mm)

**问题**: 
- `transaction.transactionReceipt` 属性从 iOS 7 开始废弃，可能导致 App Store 审核拒绝
- `__OSX_AVAILABLE_STARTING` 宏已废弃

**修复**:
- 使用 `[[NSBundle mainBundle] appStoreReceiptURL]` 读取 App 收据
- 添加空值检查和降级处理
- 移除 `__OSX_AVAILABLE_STARTING` 宏
- 保持 C# 接口不变，无需修改调用方

**注意**: 服务端验证逻辑可能需要相应调整。旧版发送的是单条交易收据，新版发送的是整包 App 收据。如果服务端使用 Apple 的 verifyReceipt 接口验证，两种格式都支持。

### 6. UnityPlugins.mm 废弃 API 修复

**问题**: `[[UIApplication sharedApplication] openURL:url]` 在 iOS 10.0 中已废弃。

**修复**: 使用 `openURL:options:completionHandler:` 新 API，并添加旧版本回退。

**涉及函数**: `joinGroup1()` (QQ 群跳转功能)

### 7. 拼写错误修复

**问题**: `GamePHP.cs` 中 `UNITY_ANDORID` 拼写错误（少了一个 R），导致该分支永远不会执行。

**修复**: `UNITY_ANDORID` → `UNITY_ANDROID`

---

## 验证结果

| 检查项 | 结果 |
|--------|------|
| UNITY_IPHONE 残留 | ✅ 0 处 |
| isNetworkError / isHttpError 残留 | ✅ 0 处 |
| UNITY_WEBPLAYER 残留 | ✅ 0 处 |
| new UnityWebRequest() 旧式构造 | ✅ 0 处 |

---

## 升级到新版 Unity 的操作步骤

### 步骤 1: 备份项目
```
复制整个 Man/Client 目录作为备份
```

### 步骤 2: 用新版 Unity 打开项目
1. 安装 Unity 2022 LTS（推荐）或 2021 LTS
2. 通过 Unity Hub 打开 `Man/Client` 目录
3. Unity 会自动弹出 "Opening Project in Non-Matching Editor Installation" 提示
4. 点击 "Continue" 等待自动升级

### 步骤 3: 处理 API Updater 提示
- Unity 会自动检测并提示更新已废弃的 API
- 点击 "I Made a Backup, Go Ahead!" 让 Unity 自动修复
- 自动修复完成后检查 Console 中是否还有错误

### 步骤 4: 切换到 iOS 平台
1. `File → Build Settings`
2. 选择 `iOS`
3. 点击 `Switch Platform`
4. 等待资源重新导入

### 步骤 5: 配置 Player Settings
1. `Edit → Project Settings → Player`
2. **Identification**:
   - Bundle Identifier: 修改为你自己的（如 com.yourcompany.sword）
   - Version: 1.0.0
   - Build: 1
   - Signing Team ID: 你的 Apple Developer Team ID
3. **Other Settings**:
   - Target minimum iOS Version: 12.0（或更高）
   - Architecture: ARM64
   - Camera Usage Description: 如不需要可留空
4. **Resolution and Presentation**:
   - 确认屏幕方向设置正确（当前为 Landscape Left + Right）

### 步骤 6: 构建 Xcode 工程
1. `File → Build Settings → Build`
2. 选择输出目录
3. 等待 Unity 生成 Xcode 工程
4. 用 Xcode 打开生成的 `Unity-iPhone.xcworkspace`
5. 在 Xcode 中配置签名和证书
6. 连接设备点击 Run 测试

---

## 已知风险和注意事项

### 高风险
1. **EasyTouch 插件版本较老** — 如果升级后出现编译错误，需要从 Asset Store 下载最新版 EasyTouch 替换
2. **iOS 内购验证逻辑** — 从交易收据改为 App 收据后，服务端 `requestIOSPay.php` 可能需要调整验证逻辑
3. **IL2CPP 编译** — 新版 Unity 默认使用 IL2CPP，某些代码可能需要添加 `[Preserve]` 或 link.xml 防止被裁剪

### 中风险
1. **图形渲染管线** — 如果项目使用了自定义 Shader，需要检查是否兼容新版 SRP
2. **视频播放** — 项目中有 Movie 文件夹，旧版 MovieTexture 已被 VideoPlayer 替代（Unity 2018+ 已提供 VideoPlayer，需要确认项目使用的是哪种）
3. **字体渲染** — 新版 Unity 的 TextMesh Pro 是默认推荐，但本项目使用的是 UGUI Text，仍然兼容

### 低风险
1. **IMGUI (OnGUI)** — GameCameraManager.cs 中使用了 GUI.DrawTexture 等，仍然支持，只是性能较差
2. **PlayerPrefs** — 完全兼容
3. **SceneManager** — 项目已使用现代 API，无需修改

---

## 建议的升级路径

| 阶段 | 目标版本 | 说明 |
|------|---------|------|
| 1 | Unity 2019.4 LTS | 最小跳跃，验证 API 兼容性 |
| 2 | Unity 2020.3 LTS | 验证 UnityWebRequest 等 API 变更 |
| 3 | Unity 2022.3 LTS | 长期支持版本，可上架 App Store |

如果项目不是特别大，也可以直接跳到 2022 LTS，遇到问题再逐一解决。

---

## 修改文件清单

```
Man/Client/Assets/Scripts/Base/GamePHP.cs
Man/Client/Assets/Scripts/Base/GameSetting.cs
Man/Client/Assets/Scripts/Data/GameIOSPayData.cs
Man/Client/Assets/Scripts/Manager/GameManager.cs
Man/Client/Assets/Scripts/Manager/GameTouchManager.cs
Man/Client/Assets/Scripts/UI/GameActiveUI.cs
Man/Client/Assets/Scripts/UI/GameHelpUI.cs
Man/Client/Assets/Scripts/UI/GameTouchCenterUI.cs
Man/Client/Assets/Scripts/Title/GameSceneTitle.cs
Man/Client/Assets/Scripts/Battle/GameBattleUserLeftUI.cs
Man/Client/Assets/Scripts/Battle/GameBattleUserRightUI.cs
Man/Client/Assets/Plugins/IOS/InAppPurchaseManager.mm
Man/Client/Assets/Plugins/IOS/UnityPlugins.mm
```

**总计**: 13 个文件

---

## iOS 真机运行修复（Unity 2022.3.62f3c1）

升级到 2022.3 LTS 后在真机（iPhone，`com.hutugames.sword`）跑通过程中，额外修复了以下问题：

### 1. 出包与启动

- `Scripts/Editor/IOSBuildPostProcess.cs`：导出后自动补 iOS 26/27 的 `UIScene` 生命周期、部署目标 15.0、签名团队、StoreKit 依赖与场景顺序
- `tools/build_ios.sh`：Unity 导出 → `xcodebuild` → `devicectl` 安装 → 启动，一条命令完成

### 2. UI 白块

| 现象 | 原因 | 修复 |
|---|---|---|
| 商店 / 行囊面板纯白 | `Image.material` 为空时回落到 Canvas 默认材质，真机上该材质无效 | `GameUI.show()` 统一补 `GameMaterial.ui`（校验 shader，无效时用 `UI/Default` 重建） |
| 行囊 / 商铺框体纯白 | 场景引用沿用原版图集切片号 `21300026/21300028`，而导出图是单图，`sprite` 为 `null`，Image 退化成白色矩形 | 仅对 `spriteMode: 1` 的图把 `fileID` 改为 `21300000`（6 处） |
| 打开行囊整体发白 | `GameUIFade` 淡出结束未应用最终 alpha，停在 0.12，下次打开不恢复 | 收尾时先 `updateUIStage()` 再结束；`GameItemUI.show()` 补 `showFade()` |

### 3. 素材串图

按 `GameAnimation.sprites[]` 的真实引用重建并逐帧比对 76671 帧，修正 172 帧同名错图（详见资源映射文档 3.3）。

### 4. 地图

`GameRPGManager` 取图顺序改为 `Texture/Rpg/<Name>` → `Texture/MapRaw/<Name>` → `Texture/Map/Stage<NN>/map<NN>` → 按名字数字回退；`MapRaw/` 存放 APK 未裁切原图，避免裁切版同名覆盖。

### 5. UI 帧动画偏移

`GameAnimation` 的 UI 分支锚点统一设为中心 (0.5, 0.5)，修复商店 / 营地 UI 帧动画被推出屏幕。

### 6. 快速存读档

新增 `Scripts/Save/GameQuickSave.cs`、`Scripts/UI/GameQuickSaveUI.cs`，按钮跟随虚拟按钮显示与隐藏：

- 营地 / 城镇 / 行囊 → 独立文件 `QuickSave.dat`（复用原版存档字节格式），**不占用也不读写原版 5 个存档槽**
- 城镇额外记录 `activeID` + 格子坐标 + 朝向（`QuickSave.rpg`），读档直接回到原位置
- 战斗 → 独立文件 `QuickBattle.dat`，不覆盖原版 `SaveBattle.dat`
- 为此把原 `save()` / `load()` 的序列化抽成 `buildSaveBytes()` / `applySaveBytes()`，两条路径共用同一份格式，避免日后漂移

### 7. 期望与现实

- `Resources.Load` 的 UI 贴图路径大小写不敏感，但**同目录仅大小写不同的文件名**会互相覆盖，导出时必须按小写判重
- 真机上缺失 sprite 的 Image 表现为**纯白矩形**而不是粉红错误材质，排查时容易被误判成"贴图没加载"，优先看日志里的 `sprite=null`
