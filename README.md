<div align="center">
  <img src="src/YCPLauncher/Assets/logo.png" alt="YCP Launcher" width="116"/>

  # YCP Launcher

  **为 CS2 社区赛事打造的 Windows 启动与比赛管理客户端**

  [![Version](https://img.shields.io/badge/version-1.1.8-55C3E3?style=for-the-badge)](https://github.com/sun20101227/YCPLauncher/releases/latest)
  ![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011-147D9A?style=for-the-badge&logo=windows)
  ![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
  ![Platform](https://img.shields.io/badge/Platform-x64-202E3C?style=for-the-badge)

  <p>
    赛事账号登录 · 房间匹配 · 阵营与席位管理 · CS2 一键进服 · 内置直播 · 自动更新
  </p>
</div>

---

## 项目简介

YCP Launcher 是 YACHIYO CUP 使用的 CS2 社区赛事客户端。它将选手认证、比赛房间、服务器分配、游戏启动、赛事直播和客户端更新整合在一个轻量的 Windows 桌面应用中。

客户端基于 **WPF / .NET 8** 构建，并提供安装版、便携版以及 CounterStrikeSharp 名称同步插件。

## 核心功能

| 模块 | 功能 |
| --- | --- |
| 账号与安全 | 赛事账号认证、DPAPI Token 加密缓存、修改密码 |
| 比赛房间 | 创建或加入房间、选择阵营与席位、准备和主席开赛 |
| 游戏启动 | 自动识别 Steam/CS2 路径，多阶段启动并连接赛事服务器 |
| 数据面板 | 选手资料、比赛数据、历史战绩与赛事公告 |
| 赛事直播 | 基于 LibVLC 的低延迟直播播放 |
| 客户端维护 | GitHub Release 更新、安装覆盖、便携运行和完整卸载 |
| 游戏插件 | CounterStrikeSharp 玩家名称与阵营同步 |

## 界面与性能

- 深色与浅色双主题
- 动态背景与主题感知遮罩
- 支持系统“减少动画”和低性能设备自动降级
- 视频解码、页面轮询和导航生命周期优化
- Windows 11 原生窗口样式与托盘驻留

## 下载与运行

前往仓库的 **Releases** 页面下载：

- `YCPInstaller.exe`：推荐，提供安装、覆盖更新和卸载支持
- `YCPLauncher_Portable_v1.1.8.zip`：免安装便携版
- `YCPNameSyncPlugin_v1.1.8.zip`：CounterStrikeSharp 名称同步插件

运行环境：

- Windows 10 / Windows 11 x64
- .NET 8 Desktop Runtime x64
- Steam 与 Counter-Strike 2

> 便携版必须完整解压后运行。请勿仅复制 `YCPLauncher.exe`，动态背景、LibVLC 和运行依赖需要保留在同一目录。

## 从源码构建

需要安装 Windows 10/11 与 .NET 8 SDK。

```powershell
git clone https://github.com/sun20101227/YCPLauncher.git
cd YCPLauncher
.\scripts\build.ps1
```

构建输出：

```text
artifacts/
├── installer/YCPInstaller.exe
├── portable/YCPLauncher/YCPLauncher.exe
├── YCPLauncher_Portable_v1.1.8.zip
└── app/
```

打包 CounterStrikeSharp 插件：

```powershell
.\scripts\package-plugin.ps1
```

## 项目结构

```text
.
├── src/
│   ├── YCPLauncher/       # WPF 主程序
│   ├── YCPInstaller/      # WPF 安装与更新程序
│   └── YCPUninstaller/    # WinForms 卸载程序
├── plugins/
│   └── YCPNameSync/       # CounterStrikeSharp 插件
├── tools/
│   ├── ProxyTest/         # GitHub 下载链路测试
│   └── TestPath/          # Steam / CS2 路径诊断
├── scripts/               # 构建、打包和 GitHub Release 脚本
├── docs/                  # 发布说明与历史资料
└── Directory.Build.props  # 全局版本及程序集信息
```

PHP 服务端和旧版网站项目不在此仓库维护。本仓库仅包含 Windows 客户端、安装器、卸载器、游戏插件和相关构建工具。

## 发布流程

版本号统一维护在 `Directory.Build.props`。完整构建和 GitHub Release 流程请参阅：

[`docs/打包与发布说明.md`](docs/打包与发布说明.md)

---

<div align="center">
  <sub>© 2026 YACHIYO CUP · Built for the community</sub>
</div>
