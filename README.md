# OPL Android

OPL 的 Android 客户端工程。当前版本提供 UID 隧道配置、游戏预设、本机 UID 管理和前台服务生命周期。

## 构建

推送到 `master`、创建 Pull Request 或在 Actions 页面手动运行 **Build Android APK**，GitHub Actions 会生成 `app-debug.apk` 产物。构建不依赖本机 Android SDK 或 Gradle。

## 本地开发

使用 Android Studio 打开仓库根目录。要求 JDK 17、Android SDK 35 和 Android 8.0（API 26）以上的 ARM64 设备。

## 网络核心

此提交完成 Android UI、配置持久化和前台服务骨架。实际 OpenP2P 网络核心需要以 JNI 库形式接入，不能将 Windows 的 `openp2p.exe` 直接带入 Android 应用。
