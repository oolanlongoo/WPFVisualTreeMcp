# 入门指南

本指南帮助你配置并开始使用 WpfVisualTreeMcp，让 AI 编程助手检查 WPF 应用。

## 环境要求

- **Windows 10/11** — WPF 仅在 Windows 上运行
- **.NET 10.0 SDK** — [下载](https://dotnet.microsoft.com/download/dotnet/10.0)（仅运行可用 Desktop Runtime）
- **WPF 应用** — 自己的应用，或仓库自带示例
- **兼容 MCP 的 AI 客户端** — Claude Code、Cursor 等

## 安装

### 方式 1：.NET 工具（推荐）

```bash
dotnet tool install -g WpfVisualTreeMcp
# MCP 客户端也可通过 dnx 直接启动：
dnx WpfVisualTreeMcp
```

### 方式 2：下载 Release

1. 打开 [GitHub Releases](https://github.com/faze79/WpfVisualTreeMcp/releases)
2. 下载 `WpfVisualTreeMcp-vX.X.X-win-x64.zip` 并解压
3. 使用其中的 `WpfVisualTreeMcp.Server.exe`

### 方式 3：源码构建

```bash
git clone https://github.com/faze79/WpfVisualTreeMcp.git
cd WpfVisualTreeMcp
dotnet build -c Release
```

可执行文件通常在：

```
src/WpfVisualTreeMcp.Server/bin/Release/net10.0/WpfVisualTreeMcp.Server.exe
```

或发布到 `./publish`：

```bash
dotnet publish src/WpfVisualTreeMcp.Server/WpfVisualTreeMcp.Server.csproj -c Release -o ./publish
```

## 配置 MCP 客户端

路径请使用**绝对路径**和正斜杠 `/`。改配置后重启客户端。

### Claude Code

```bash
claude mcp add wpf-visual-tree -- C:/path/to/WpfVisualTreeMcp.Server.exe
claude mcp add --scope user wpf-visual-tree -- C:/path/to/WpfVisualTreeMcp.Server.exe
claude mcp list
```

或项目根目录 `.mcp.json`：

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

### Cursor

编辑 `.cursor/mcp.json`，内容同上。

## 两种接入目标应用的方式

### A. 自动注入（无需改源码）

对任意正在运行的 **.NET Framework WPF** 应用：

1. 用 `wpf_list_processes` 找到 PID
2. `wpf_attach(process_id=..., auto_inject=true)`

需要发布包中带有 `native/x64`（或 x86）下的引导 DLL。自 v0.6.0 起，64 位服务端可通过 `WpfInjectorHelper.exe` 注入 32 位进程。

### B. 自托管（推荐用于自己的应用）

1. 引用 `WpfVisualTreeMcp.Inspector` 项目（或将来的 Inspector NuGet 包）

2. 在 `App.xaml.cs`：

```csharp
using System.Diagnostics;
using System.Windows;
using WpfVisualTreeMcp.Inspector;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        InspectorService.Initialize(Process.GetCurrentProcess().Id);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        InspectorService.Instance?.Dispose();
        base.OnExit(e);
    }
}
```

示例应用 `samples/SampleWpfApp` 已配置好自托管。

## 快速教程

### 1. 启动示例应用

```bash
dotnet run --project samples/SampleWpfApp
```

### 2. 让 Agent 列出进程

```
列出我可以检查的所有正在运行的 WPF 应用。
```

### 3. 附加

```
附加到 SampleWpfApp，开始检查。
```

注入场景请明确要求 `auto_inject=true`。

### 4. 探索与诊断

```
显示主窗口可视化树。
显示 SubmitButton 的属性。
这个应用有没有绑定错误？
找到可见文本包含「Save」的按钮。
```

### 5. 交互闭环（可选）

```
截一张当前窗口的图。
在用户名框输入测试文本并提交。
改一下某个 Margin，用 snapshot/diff 看布局有没有变化，再 revert。
```

## 常见任务提示语

**布局问题**

```
给出 ContentGrid 的布局信息，实际尺寸和 Margin 是多少？
```

**查找元素**

```
找出所有 TextBox。
找出 IsEnabled 为 false 的元素。
```

**绑定分析**

```
显示 UserNameTextBox 上的全部绑定。
SelectedItem 的绑定源是什么？DataContext 是什么类型？
```

## 排错

### 「找不到 WPF 应用」

- 确认目标已启动且主窗口可见
- 确认是 WPF（.NET Framework 或带 WPF 的桌面应用）

### 「附加失败」/「Inspector 未加载」

- 自托管：确认调用了 `InspectorService.Initialize`
- 注入：确认 `auto_inject=true`，且 `publish/native/{x64|x86}` 中有引导 DLL
- 查看日志：`%TEMP%\WpfInspector_Debug.log`、`%TEMP%\WpfInspectorBootstrapper.log`

### 「连接超时」/「通信错误」

- 命名管道名格式：`wpf_inspector_{pid}`
- 连接超时约 5s，请求超时约 30s
- 服务端日志：`%LOCALAPPDATA%\WpfVisualTreeMcp\logs\`

### 「元素未找到」

- 句柄仅在当前 Inspector 会话内有效（格式 `elem_XXXXXXXX`）
- 目标重启或元素被 GC 后句柄失效 → 重新 `wpf_find_elements`
- **不要**把 `wpf_attach` 返回的 `main_window_handle`（`window_0x…`）当作元素句柄传给其他工具

### 绑定路径错误

- 路径拼写错误、源属性不存在、DataContext 为 null — 用 `wpf_get_data_context` + `wpf_get_bindings` 排查

## 最佳实践

1. 先查可视化树或用 `text`/`type_name` 搜索，不要一上来整树导出
2. UI 异常优先看绑定错误
3. 点开 Popup / 下拉 / 右键菜单后，截图用 `mode=screen`
4. 物理点击前元素会自动滚入视图；双击/右键始终走物理输入
5. `wpf_set_property` 会覆盖绑定为本地值；用完请 `wpf_revert_property`
6. 异步 UI 变化后用 `wpf_wait_for_element`，不要 sleep 盲等

## 下一步

- [工具参考](TOOLS_REFERENCE.md)
- [架构说明](ARCHITECTURE.md)
- [CLI 参考](CLI.md)
- 用带故意绑定错误的示例应用练手
