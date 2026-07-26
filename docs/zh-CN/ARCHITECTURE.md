# 架构说明

本文描述 WpfVisualTreeMcp 的技术架构。

## 概述

项目作为 AI Agent 与运行中 WPF 应用之间的桥梁，采用**多进程**设计：服务端与目标进程隔离，通过命名管道通信，尽量不影响目标稳定性。

```
AI Agent（Claude Code / Cursor 等）
        │ MCP：JSON-RPC over stdio
        ▼
MCP Server（.NET 10.0）
  ├─ WpfTools（26 个工具）
  ├─ ProcessManager（发现 / 附加 / 注入协调）
  └─ NamedPipeBridge（IPC）
        │ 命名管道：wpf_inspector_{pid}
        ▼
目标 WPF 应用
  └─ Inspector DLL（.NET Framework 4.8 / 亦有 CoreCLR 目标）
       ├─ TreeWalker（可视化树 + Adorner / Popup）
       ├─ ScreenshotCapture
       ├─ PropertyReader / PropertyWriter
       ├─ BindingAnalyzer
       ├─ ControlInteractor
       └─ IpcServer
```

## 主要组件

### 1. MCP Server（`WpfVisualTreeMcp.Server`）

- **技术：** .NET 10.0，官方 C# MCP SDK
- **职责：** stdio 上的 MCP 协议、工具注册、进程管理、转发检查请求
- **双模式：** 无参数 → MCP 服务端；有已识别子命令 → CLI（`CliRunner`）
- **关键类：** `Program`、`WpfTools`、`ProcessManager`、`NamedPipeBridge`、`CliRunner`

### 2. Inspector（`WpfVisualTreeMcp.Inspector`）

加载进目标进程，执行实际检查与交互。

- **技术：** 主要为 .NET Framework 4.8（兼容经典 WPF）；另有 `net10.0-windows` 目标
- **职责：** VisualTreeHelper / LogicalTreeHelper、依赖属性与绑定、资源与样式、截图、点击与输入、属性写入与撤销、命名管道服务端
- **关键类：** `InspectorService`、`TreeWalker`、`PropertyReader`、`PropertyWriter`、`BindingAnalyzer`、`ControlInteractor`、`ScreenshotCapture`、`IpcServer`

### 3. 注入链路

| 组件 | 作用 |
|------|------|
| `WpfVisualTreeMcp.Injector` | CreateRemoteThread + LoadLibrary 等托管注入逻辑 |
| `WpfVisualTreeMcp.Bootstrapper` | 原生 C++ DLL，在目标中托管 CLR 并加载 Inspector |
| `WpfVisualTreeMcp.InjectorHelper` | 32 位 .NET 10 辅助进程，供 64 位服务端向 32 位目标注入 |

### 4. Shared（`WpfVisualTreeMcp.Shared`）

IPC 消息契约与共享模型（`IpcMessages` 等）。

## 通信与序列化

- 管道名：`wpf_inspector_{pid}`
- 连接超时：约 5 秒；请求超时：约 30 秒
- 使用直接字节 I/O（避免 .NET Framework 4.8 下 StreamReader/Writer 死锁）
- Inspector 解析前会剥离 UTF-8 BOM
- **重要：** `IpcSerializer.SerializeRequest` 必须按请求的**运行时类型**序列化；若把派生请求声明为基类 `IpcRequest`，System.Text.Json 会丢掉过滤条件、句柄等字段（有回归测试）

`IpcServer` 接受**并发**管道连接（每客户端独立任务）；真正的 UI 操作仍在 Dispatcher 上串行。长时间 `wpf_wait_for_element` 在阻塞 Dispatcher 路径**之前**于后台轮询，因此不会堵死其他命令。其超时钳制在 25s 以内，低于 IPC 30s 上限。

## 线程模型

WPF 为 STA：可视化树操作必须在 UI Dispatcher 上执行。

- Inspector 用 `Task.Run` + `Dispatcher.Invoke`，避免阻塞 IPC 线程
- UI 操作超时约 10 秒
- `wpf_wait_for_element`：后台短时 Invoke 检查 + `Task.Delay` 间隔

## 元素句柄

- 格式：`elem_{counter:X8}`（如 `elem_00000052`）
- 仅在同一 Inspector 会话（同一目标进程生命周期）内有效
- 目标重启后全部失效
- 句柄缓存使用弱引用：离开可视化树的元素可被 GC，句柄随后过期
- `wpf_attach` 返回的 `main_window_handle`（`window_0x…`）是 Win32 HWND 元数据，**不能**当作 `element_handle` 传给其他工具

## 可视化树遍历

- 标准子节点：`VisualTreeHelper`
- 另枚举 `AdornerLayer` 装饰器（如 Fluent.Ribbon Backstage）
- 进入 `Popup` 子树（独立可视化树）
- 无 `root_handle` 时，`FindElements` 跨所有打开窗口搜索
- 查询过滤器（AND）：`type_name`、`element_name`、`text`、`property_filter`、`visible_only`
- 默认 `max_depth` 为 25（可配 1–100）

## 截图模式

| 模式 | 行为 |
|------|------|
| `render`（默认） | RenderTargetBitmap + VisualBrush；窗口被遮挡也可工作；**看不到**打开的 Popup / 菜单 |
| `screen` | GDI BitBlt 屏幕像素；含 Popup、下拉、右键菜单、ToolTip；窗口需可见且不被遮挡 |

支持 DPI；超过 `max_width`/`max_height`（默认 1920×1080）会缩小。MCP 侧返回 base64 PNG 图像内容块。

## 状态变更与安全

**只读**为主。会改变应用状态的工具：

- `wpf_click_element`、`wpf_select_item`、`wpf_set_text`、`wpf_send_keys`
- `wpf_set_property`、`wpf_revert_property`（后者用于撤销前者）

`wpf_set_property` 通过 TypeConverter 强制转换；写入前记录绑定/本地值/未设置状态，以便 `wpf_revert_property` 按会话撤销栈恢复。

进程隔离：MCP Server 不在目标进程内跑。注入仅面向合适的 .NET WPF 进程；管道名含 PID 防冲突。

## 日志位置

| 组件 | 路径 |
|------|------|
| MCP Server | `%LOCALAPPDATA%\WpfVisualTreeMcp\logs\mcp-server-YYYYMMDD.log` |
| Inspector | `%TEMP%\WpfInspector_Debug.log` |
| Bootstrapper | `%TEMP%\WpfInspectorBootstrapper.log` |

**stdout 必须保持干净**（留给 JSON-RPC）；诊断写 stderr / 日志文件。

## 性能注意点

- 大树使用深度限制与结果上限（`max_results`）
- 属性按需读取；昂贵项可选
- 句柄会话内缓存；属性值不缓存（始终最新）
- `wpf_snapshot` / `wpf_diff` 用句柄对齐前后状态，精确度量改动副作用

## 源码位置速查

| 组件 | 路径 |
|------|------|
| 服务端入口 | `src/WpfVisualTreeMcp.Server/Program.cs` |
| 工具定义 | `src/WpfVisualTreeMcp.Server/WpfTools.cs` |
| CLI | `src/WpfVisualTreeMcp.Server/Cli/CliRunner.cs` |
| IPC 桥 | `src/WpfVisualTreeMcp.Server/Services/NamedPipeBridge.cs` |
| Inspector 入口 | `src/WpfVisualTreeMcp.Inspector/InspectorService.cs` |
| IPC 消息 | `src/WpfVisualTreeMcp.Shared/Ipc/IpcMessages.cs` |

更细的路线图见 [docs/ROADMAP.md](../ROADMAP.md)。
