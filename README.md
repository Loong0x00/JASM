# JASM — Just Another Skin Manager · Linux / Avalonia fork

> **本仓库是 JASM 的 Linux 移植 fork**：把界面从 WinUI 3（锁死 Windows）用 [Avalonia](https://avaloniaui.net/) 重写，**在 Linux 上原生运行、无需 Wine**；跨平台逻辑库 `GIMI-ModManager.Core` 原样复用，只替换了界面层。
>
> **传承**：原项目 [Jorixon/JASM](https://github.com/Jorixon/JASM)（GPL-3.0，已停更）→ 中文 fork [Moonholder/JASM](https://github.com/Moonholder/JASM)（本 fork 基于它，增加了游戏与中文）→ **本仓库：Avalonia 跨平台前端**。

JASM 是一个**皮肤管理器**：帮你在磁盘上按角色整理、启用/禁用 3Dmigoto / XXMI 模组。它**不向游戏注入内容**——注入由 XXMI / 3Dmigoto 加载器完成。支持原神（GIMI）、崩坏：星穹铁道（SRMI）、绝区零（ZZMI）、鸣潮（WWMI）、明日方舟：终末地（EFMI）五个游戏。

---

## 🐧 Linux 版（本 fork）

**构建 / 运行**（需 [.NET 10 SDK](https://dotnet.microsoft.com/download)）：

```bash
cd src/GIMI-ModManager.Avalonia
dotnet run -c Release
# 或产出独立可执行文件：
dotnet publish -c Release -r linux-x64 --self-contained false -o out && ./out/JASM.Avalonia
```

首次启动会走设置向导：选游戏 + 指定 Mods 文件夹（XXMI/3Dmigoto 文件夹可留空）。

**功能**

- 5 个游戏（GIMI/SRMI/ZZMI/WWMI/EFMI）+ 游戏内切换（切换走进程重启，与原版行为一致）
- 角色网格：分类标签（角色/武器/NPC/物件）、元素·武器筛选、排序、置顶、隐藏角色、mod 数徽章、搜索
- 角色详情两栏（mod 列表 + ModPane）：启用/禁用（改 `DISABLED_` 前缀）、多选批量启用/禁用/删除/移动、页内搜索
- ModPane：封面图查看+设置、名字/作者/链接/描述编辑、按键切换（keyswap `.ini`）编辑器、复制路径
- Mod 安装器：从**文件夹**或 **`.zip`/`.rar`/`.7z` 压缩包**导入 + 目标角色选择
- 预设（建自当前/应用/重命名/复制/删/只读）、模组总览、导出全部模组、主题（跟随系统/浅色/深色）、语言（中/英）

**说明**

- **设置目录**：`~/.local/share/JASM/`（删掉即重走首次设置）。
- **删除模组**：进 freedesktop XDG 回收站，可恢复。
- **无需 Elevator**：Linux 下启用/禁用直接改文件夹名（加/去 `DISABLED_` 前缀）即生效，不需要 Windows 版那个提权辅助进程。
- 完整开发/架构说明见 [`src/GIMI-ModManager.Avalonia/README.md`](src/GIMI-ModManager.Avalonia/README.md)。

**尚未移植**（重 / 联网 / niche）：GameBanana 内置浏览器、自动查 mod 更新、自定义角色创建、命令运行器、拖放安装、加密压缩包密码管理、从应用内启动游戏 / 3Dmigoto（Linux 走 Proton，逻辑不同）、F10 向游戏发键刷新。

**通用 Tips**

- 每个 mod 的设置存在其文件夹内、以 `.JASM_` 前缀命名，导出时可忽略。
- 预览图优先识别 `cover` / `.jasm_cover` / `preview` 前缀的图片。
- 查看/编辑模组按键切换（keyswap）：在角色详情右侧 ModPane 的按键切换面板里。

---

## 🪟 原版 Windows（WinUI）说明

> 以下是上游 [Moonholder/JASM](https://github.com/Moonholder/JASM) 的 **Windows / WinUI 版本**说明，保留供 Windows 用户参考。**Linux 用户请用上面的 Linux 版**——以下 `.exe`、Windows App SDK、Elevator、`%localappdata%`、单选模式、命令行、内存泄漏等**均为 Windows 版专属**，对本 fork 的 Avalonia 版不适用。

### 下载（Windows）

最新版本可从 GameBanana 或 [Releases](https://github.com/Moonholder/JASM/releases) 下载。运行 `JASM/` 文件夹中的 `JASM - Just Another Skin Manager.exe`，建议创建一个快捷方式。

### 系统要求（Windows）

- Windows 10 1809 版本或更高
- [WebP 映像扩展](https://apps.microsoft.com/detail/9pg2dk419drg?hl=zh-CN&gl=CN)（如果有角色图无法显示可安装）

### 快捷键（Windows 版）

- **空格** — 切换所选模组的启用/禁用
- **F10** — 刷新游戏中的模组（需 Elevator 提权进程 + 游戏正在运行）
- **F5** — 从磁盘刷新角色的模组；**Ctrl+F** — 聚焦搜索栏；**Esc** — 返回角色概览
- **F1** — 打开游戏内可选皮肤；**Ctrl+O** — 添加压缩包模组；**Ctrl+V** — 从剪贴板粘贴模组（GB 链接/文件夹/压缩包）

### Elevator 提权进程（Windows 版）

Elevator 是个小程序，通过命名管道接收应用发来的“1”命令后，向游戏发送 F10 键来刷新模组（在 JASM 中启用/禁用模组也会自动刷新）。用 [H.InputSimulator](https://github.com/HavenDV/H.InputSimulator) 发送键盘输入。

### 常见问题（Windows 版）

- **角色只能启用一个模组了？** → 角色详情左上角点“**显示**”，取消勾选“**单选模式**”。
- **头像左上有黄色叹号？** → 侧边栏“角色管理”里搜到该角色，勾“允许启用多个模组”（仅去掉多 mod 的警告）。
- **拖不了模组到头像上？** → 多半是以管理员权限运行导致 UIPI 隔离；改用 `Ctrl+O` 加压缩包，或左上“模组”添加。
- **JASM 打不开？** → 删除设置文件夹 `%localappdata%\JASM`（会清预设/路径等设置，**不影响模组**；建议先备份）。可先删单个游戏的设置文件夹试。

### XXMI 兼容 / 从应用启动游戏（Windows 版）

在“启动游戏”弹窗中点“创建高级命令”，参数示例：

```powershell
--nogui --xxmi GIMI
--nogui --xxmi ZZMI
--nogui --xxmi SRMI
--nogui --xxmi WWMI
--nogui --xxmi EFMI
```

### 命令行（Windows 版）

```powershell
.\'JASM - Just Another Skin Manager.exe' --help
# 示例：关闭当前实例并用选定游戏启动
.\'JASM - Just Another Skin Manager.exe' --switch --game genshin
```

### 内存占用高（Windows 版）

WinUI 在页面导航时疑似泄漏（大多是非托管内存），快速翻页会很快超过 1GB，建议重启应用。**本 fork 的 Avalonia 版不使用 WinUI，无此问题。**

---

## 许可 / License

**GNU GPL-3.0**，与上游 JASM 一致。原始设计、内置游戏资产数据、核心模组管理逻辑归上游作者（Jorixon / Moonholder）所有；本 fork 仅把锁死 Windows 的 WinUI 前端替换为跨平台的 Avalonia 前端。
