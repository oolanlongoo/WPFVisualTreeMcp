# WpfVisualTreeMcp

<!-- mcp-name: io.github.faze79/wpf-visual-tree -->

[![Build](https://github.com/faze79/WpfVisualTreeMcp/actions/workflows/build.yml/badge.svg)](https://github.com/faze79/WpfVisualTreeMcp/actions/workflows/build.yml)
[![Release](https://img.shields.io/github/v/release/faze79/WpfVisualTreeMcp)](https://github.com/faze79/WpfVisualTreeMcp/releases)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![MCP Compatible](https://img.shields.io/badge/MCP-Compatible-green)](https://modelcontextprotocol.io/)

[English](README.md) | **简体中文**

> **让 AI Agent 能看见、调试并驱动正在运行的 WPF 应用。** 面向 AI 的 Snoop + Playwright，通过 Model Context Protocol 暴露 —— 可视化树、数据绑定、截图、点击、文本输入与快捷键，对任意运行中的 WPF 进程生效，无需改目标应用源码。

![演示：AI Agent 驱动 WPF 应用并定位绑定错误](docs/assets/demo.gif)

*Agent 按可见文本查询控件、填写表单、选择列表项、点击提交 —— 再拉取绑定错误解释为何 Status 为空。每一步都是一次 MCP 工具调用（或 CLI 命令）。*

## 60 秒上手

1. 下载并解压 [最新 Release](https://github.com/faze79/WPFVisualTreeMcp/releases)（需 [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)）。
2. 在 MCP 客户端中注册。Claude Code 可写入 `.mcp.json`：
   ```json
   {
     "mcpServers": {
       "wpf-visual-tree": {
         "command": "C:/path/to/WpfVisualTreeMcp.Server.exe",
         "args": []
       }
     }
   }
   ```
3. 对 Agent 说：*「看看我正在运行的 WPF 应用，为什么 Save 按钮是禁用的？」*

更喜欢终端？同一可执行文件就是完整 CLI：`WpfVisualTreeMcp.Server.exe help`。

## 项目简介

**WpfVisualTreeMcp** 是一个 [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) 服务端，让 AI 编程助手（Claude Code、Cursor、GitHub Copilot 等）能够检查并与正在运行的 WPF 应用交互。相当于给 AI 装上类似 [Snoop WPF](https://github.com/snoopwpf/snoopwpf) 或 Visual Studio Live Visual Tree 的能力。

### 为什么有用

传统 WPF UI 调试依赖人工用专用工具检查。本项目把这些能力接到 MCP，使 Agent 可以：

- 在代码审查中理解 UI 结构
- 自动诊断绑定错误
- 给出布局问题修复建议
- 协助 UI 重构
- 实时分析可视化树层级

## 功能概览

### 核心检查
- **进程发现** — 列出可检查的运行中 WPF 应用
- **可视化树导航** — 遍历完整可视化树
- **属性检查** — 读取任意 UI 元素的依赖属性

### 绑定与资源
- **绑定分析** — 支持 Converter、StringFormat、FallbackValue、MultiBinding
- **DataContext 检查** — 类型、属性、INPC、继承链
- **绑定错误捕获** — 自动捕获并分类报告
- **资源 / 样式检查** — 资源字典与已应用样式、模板

### 搜索与监控
- **元素查询** — 按类型、x:Name、可见文本、属性值、可见性过滤
- **深度搜索** — 含 AdornerLayer、Popup
- **属性监视** — 监控属性变化

### 交互与导出
- **截图** — `render`（离屏渲染）/ `screen`（含 Popup、下拉、右键菜单）
- **元素高亮** — 在运行中应用内高亮定位
- **点击 / 选中 / 设文本 / 发快捷键** — 可改变应用状态
- **实时改属性 / 回滚** — `wpf_set_property` + `wpf_revert_property`
- **快照 / Diff** — `wpf_snapshot` + `wpf_diff` 验证改动是否生效
- **等待条件** — `wpf_wait_for_element`，无需 sleep 重试循环
- **自动注入** — 向运行中进程注入 Inspector；自 v0.6.0 支持跨架构（x64 服务端 → x86 目标）

### 双模式 CLI
同一 `WpfVisualTreeMcp.Server.exe`：无参数时为 MCP stdio 服务端；带已识别子命令时为一次性 CLI。输出 JSON 到 stdout，诊断信息到 stderr。详见 [CLI 参考](docs/zh-CN/CLI.md)。

## 与同类方案对比

| | **WpfVisualTreeMcp** | [Snoop](https://github.com/snoopwpf/snoopwpf) | FlaUI / WinAppDriver | 通用电脑操控（截图+鼠标） |
|---|---|---|---|---|
| 使用者 | **AI（MCP）+ 人（CLI）** | 人（GUI） | 测试代码 | AI |
| 可视化树、依赖属性 | ✅ | ✅ | ❌（仅 UIA） | ❌ |
| 绑定 / 绑定错误 / DataContext | ✅ | ✅ | ❌ | ❌ |
| 按可见文本 / 属性找控件 | ✅ | 手动 | 部分 | 像素猜测 |
| 点击 / 输入 / 选中 / 快捷键 | ✅ | ❌ | ✅ | ✅（盲目） |
| 等待 UI 条件 | ✅ | ❌ | ✅ | ❌ |
| 元素截图 + Popup 感知 | ✅ | ❌ | 部分 | 仅全屏 |
| 无需改目标源码 | ✅（自动注入） | ✅ | ✅ | ✅ |

多数 WPF MCP 基于 **UI Automation（FlaUI）**：只能看到无障碍树，要看绑定与 DataContext 往往需要在目标应用里装探针。本项目走 Snoop 路线：**运行时注入**，读取真实可视化树，**零改动目标应用**。

## 安装

### 环境要求

- Windows 10/11
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)（开发）或 Desktop Runtime（仅运行）
- 要检查的 WPF 应用

### 安装方式

#### 方式 1：.NET 工具 / NuGet（推荐）

```bash
dotnet tool install -g WpfVisualTreeMcp     # 安装 wpfinspect 命令
dnx WpfVisualTreeMcp                        # 直接以 MCP stdio 服务端运行
```

#### 方式 2：下载 Release

从 [GitHub Releases](https://github.com/faze79/WpfVisualTreeMcp/releases) 下载 `WpfVisualTreeMcp-vX.X.X-win-x64.zip` 并解压。

#### 方式 3：源码构建

```bash
git clone https://github.com/faze79/WpfVisualTreeMcp.git
cd WpfVisualTreeMcp
dotnet build -c Release
```

### 配置 MCP 客户端

#### Claude Code

```bash
claude mcp add wpf-visual-tree -- C:/path/to/WpfVisualTreeMcp.Server.exe
# 或全局：
claude mcp add --scope user wpf-visual-tree -- C:/path/to/WpfVisualTreeMcp.Server.exe
```

或在项目根目录 `.mcp.json`：

```json
{
  "mcpServers": {
    "wpf-visual-tree": {
      "command": "C:/path/to/WpfVisualTreeMcp.Server.exe",
      "args": []
    }
  }
}
```

#### Cursor

写入 `.cursor/mcp.json`（路径同上）。配置后重启客户端。路径请用**绝对路径**和正斜杠 `/`。

### 自托管模式（可选）

在自己的 WPF 应用启动时初始化 Inspector（无需注入）：

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    InspectorService.Initialize(Process.GetCurrentProcess().Id);
}
```

详细步骤见 [入门指南](docs/zh-CN/GETTING_STARTED.md)。

## 用法示例

对 Agent 说：

```
列出所有正在运行的 WPF 应用。
附加到 MyApp.exe，显示主窗口可视化树。
检查是否有绑定错误并解释原因。
找所有 IsEnabled=false 的 Button。
把当前窗口可视化树导出为 JSON。
```

## 架构一览

```
AI Agent
  ↓ MCP（JSON-RPC over stdio）
MCP Server（.NET 10）
  ├─ WpfTools（26 个工具）
  ├─ ProcessManager
  └─ NamedPipeBridge
        ↓ 命名管道 wpf_inspector_{pid}
目标 WPF 应用
  └─ Inspector DLL（.NET 4.8）
```

多进程隔离；除点击/选中/设文本/快捷键/设属性/回滚外，其余为只读。详见 [架构说明](docs/zh-CN/ARCHITECTURE.md)。

## 可用工具（26）

| 工具 | 说明 |
|------|------|
| `wpf_list_processes` | 列出运行中的 WPF 应用 |
| `wpf_attach` | 附加（`auto_inject=true` 可自动注入） |
| `wpf_get_visual_tree` | 获取可视化树（默认深度 25） |
| `wpf_get_element_properties` | 读取依赖属性 |
| `wpf_find_elements` | 按类型 / 名称 / 文本 / 属性查询 |
| `wpf_find_elements_deep` | 深度查询（无结果上限） |
| `wpf_capture_screenshot` | 截图（`render` / `screen`） |
| `wpf_get_bindings` | 数据绑定 |
| `wpf_get_binding_errors` / `wpf_clear_binding_errors` | 绑定错误 |
| `wpf_get_data_context` | DataContext |
| `wpf_get_resources` / `wpf_get_styles` | 资源与样式 |
| `wpf_watch_property` | 监视属性 |
| `wpf_snapshot` / `wpf_diff` | 快照与 Diff |
| `wpf_set_property` / `wpf_revert_property` | 实时改属性 / 回滚（会改状态） |
| `wpf_highlight_element` | 高亮 |
| `wpf_click_element` | 点击（会改状态） |
| `wpf_select_item` | 列表/下拉/Tab 选中（会改状态） |
| `wpf_wait_for_element` | 等待元素条件 |
| `wpf_set_text` / `wpf_send_keys` | 设文本 / 快捷键（会改状态） |
| `wpf_get_layout_info` | 布局信息 |
| `wpf_export_tree` | 导出 XAML / JSON |

完整参数与示例见 [工具参考](docs/zh-CN/TOOLS_REFERENCE.md)。

## 中文文档索引

| 文档 | 内容 |
|------|------|
| [入门指南](docs/zh-CN/GETTING_STARTED.md) | 安装、配置、教程、排错 |
| [架构说明](docs/zh-CN/ARCHITECTURE.md) | 组件、IPC、线程模型 |
| [工具参考](docs/zh-CN/TOOLS_REFERENCE.md) | 全部 MCP 工具参数 |
| [CLI 参考](docs/zh-CN/CLI.md) | 命令行用法与示例 |
| [路线图（英文）](docs/ROADMAP.md) | 后续计划 |

## 开发

```bash
dotnet build WpfVisualTreeMcp.sln
dotnet test WpfVisualTreeMcp.sln
dotnet run --project samples/SampleWpfApp
dotnet publish src/WpfVisualTreeMcp.Server/WpfVisualTreeMcp.Server.csproj -c Release -o ./publish
```

欢迎提交 Pull Request。

## 致谢与许可

灵感来自 [Snoop WPF](https://github.com/snoopwpf/snoopwpf)；基于 Anthropic 的 [Model Context Protocol](https://modelcontextprotocol.io/)。MIT License，见 [LICENSE](LICENSE)。
