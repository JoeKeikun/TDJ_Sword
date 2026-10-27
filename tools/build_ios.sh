#!/usr/bin/env bash
# 一键出 iOS 真机包并启动。
#
# 前置：Assets/Resources/Texture 等导出素材必须已存在（新环境先跑 tools/export_assets.py）。
#
# 可用环境变量：
#   TDJ_IOS_OUT    Xcode 工程导出目录，默认 /Users/joekeikun/tdj_ios_device_build
#   TDJ_UNITY      Unity 可执行文件，默认 /Applications/Unity/Hub/Editor/2022.3.62f3c1/Unity.app/Contents/MacOS/Unity
#   TDJ_DEVICE     设备 UDID，默认 00008130-001C2C9C3E98001C
#   TDJ_BUNDLE_ID  Bundle ID，默认 com.hutugames.sword
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CLIENT="$REPO/Man/Client"
OUT="${TDJ_IOS_OUT:-/Users/joekeikun/tdj_ios_device_build}"
UNITY="${TDJ_UNITY:-/Applications/Unity/Hub/Editor/2022.3.62f3c1/Unity.app/Contents/MacOS/Unity}"
DEVICE="${TDJ_DEVICE:-00008130-001C2C9C3E98001C}"
BUNDLE_ID="${TDJ_BUNDLE_ID:-com.hutugames.sword}"

if [ ! -d "$CLIENT/Assets/Resources/Texture" ]; then
    echo "缺少导出素材 Assets/Resources/Texture，请先运行：python3 tools/export_assets.py" >&2
    exit 1
fi

echo "==> Unity 导出 Xcode 工程"
TDJ_IOS_OUT="$OUT" "$UNITY" -batchmode -quit -buildTarget iOS \
    -projectPath "$CLIENT" \
    -executeMethod DeviceBuild.Build \
    -logFile /tmp/tdj_unity_build.log

echo "==> xcodebuild"
xcodebuild -project "$OUT/Unity-iPhone.xcodeproj" \
    -scheme Unity-iPhone \
    -configuration Debug \
    -destination "generic/platform=iOS" \
    -allowProvisioningUpdates \
    -allowProvisioningDeviceRegistration \
    -derivedDataPath "$OUT/DerivedData" \
    build

APP="$(find "$OUT/DerivedData/Build/Products" -maxdepth 2 -name '*.app' | head -1)"
echo "==> 安装 $APP"
xcrun devicectl device install app --device "$DEVICE" "$APP"

echo "==> 启动（console 输出写 /tmp/tdj_device.log）"
xcrun devicectl device process launch --device "$DEVICE" --terminate-existing --console "$BUNDLE_ID" \
    > /tmp/tdj_device.log 2>&1 &
echo "完成。日志：/tmp/tdj_device.log"
