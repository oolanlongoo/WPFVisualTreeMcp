# CLI 参考

`WpfVisualTreeMcp.Server.exe` **无参数**时作为 MCP stdio 服务端；带任意已识别子命令时进入**一次性 CLI** 模式。与 MCP 共用同一套服务，便于脚本化或在未连接 MCP 时验证链路。

全局工具名：若已 `dotnet tool install -g WpfVisualTreeMcp`，也可使用 `wpfinspect`。

## 基本约定

| 项 | 行为 |
|----|------|
| 输出 | 默认缩进 JSON 到 **stdout**；`--compact` 单行 |
| 日志 | 写到 **stderr**，保持 stdout 可解析 |
| 错误 | stdout：`{"error":"..."}`；stderr：一行明文；退出码 `1` |
| 目标 | 除 `list` / `help` 外，用 `--pid <id>` 或 `--process <name>` |
| 句柄 | 无状态进程：每次命令新建轻量会话，但句柄存在目标进程的 Inspector 中，目标不重启则跨调用仍有效 |
| 帮助 | `help` 或 `<command> --help` |

```bash
WpfVisualTreeMcp.Server.exe help
WpfVisualTreeMcp.Server.exe list
WpfVisualTreeMcp.Server.exe attach --pid 1234 --auto-inject
WpfVisualTreeMcp.Server.exe find --pid 1234 --type Button --text Save
WpfVisualTreeMcp.Server.exe screenshot --pid 1234 --out app.png
```

## 命令一览

### 进程

| 命令 | 说明 |
|------|------|
| `list` | 列出 WPF 进程 |
| `attach` | 附加；`--auto-inject` 自动注入 |

### 检查

| 命令 | 说明 |
|------|------|
| `tree` | 可视化树（`--handle`、`--depth`） |
| `props` | 依赖属性（`--handle`） |
| `find` | 查询元素（`--type`、`--name`、`--text`、`--visible-only` 等） |
| `find-deep` | 深度查询 |
| `bindings` | 绑定 |
| `binding-errors` | 绑定错误 |
| `clear-binding-errors` | 清空绑定错误列表 |
| `data-context` | DataContext |
| `resources` | 资源（`--scope`） |
| `styles` | 样式 |
| `layout` | 布局信息 |
| `watch-property` | 监视属性 |
| `highlight` | 高亮（`--duration`） |
| `export` | 导出；`--format json|xaml`，可选 `--out` |
| `screenshot` | 截图写 PNG；`--out`、`--mode render|screen` |

### 等待与度量

| 命令 | 说明 |
|------|------|
| `wait-for` | 等待元素条件（`--condition visible|exists|enabled|hidden`） |
| `snapshot` | 捕获快照（`--label`、`--handle`、`--depth`） |
| `diff` | 比较两个快照标签 |

### 会改变状态

| 命令 | 说明 |
|------|------|
| `click` | 点击；`--physical`；`--click-type double|right` |
| `select-item` | 选中；`--item-text` 或 `--index` |
| `set-text` | 设文本；`--physical` 键盘输入 |
| `send-keys` | 快捷键，如 `--keys Ctrl+S` |
| `set-property` | 实时改属性 `--property` `--value` |
| `revert-property` | 回滚；可 `--all` 或按句柄/属性过滤 |

## 典型工作流

```bash
# 1. 找 PID
WpfVisualTreeMcp.Server.exe list

# 2. 注入一次（目标进程存活期间后续命令可复用句柄）
WpfVisualTreeMcp.Server.exe attach --pid 1234 --auto-inject

# 3. 查找并检查
WpfVisualTreeMcp.Server.exe find --pid 1234 --type Button --text Save
WpfVisualTreeMcp.Server.exe props --pid 1234 --handle elem_00000052
WpfVisualTreeMcp.Server.exe binding-errors --pid 1234

# 4. 驱动 UI
WpfVisualTreeMcp.Server.exe set-text --pid 1234 --handle elem_... --text "hello"
WpfVisualTreeMcp.Server.exe select-item --pid 1234 --handle elem_... --item-text "Option A"
WpfVisualTreeMcp.Server.exe click --pid 1234 --handle elem_...
WpfVisualTreeMcp.Server.exe wait-for --pid 1234 --text OK --condition visible

# 5. 打开菜单后用 screen 模式截图
WpfVisualTreeMcp.Server.exe click --pid 1234 --handle elem_... --click-type right
WpfVisualTreeMcp.Server.exe screenshot --pid 1234 --mode screen --out menu.png

# 6. 试改属性并度量
WpfVisualTreeMcp.Server.exe snapshot --pid 1234 --label before
WpfVisualTreeMcp.Server.exe set-property --pid 1234 --handle elem_... --property Margin --value "40,40,40,40"
WpfVisualTreeMcp.Server.exe snapshot --pid 1234 --label after
WpfVisualTreeMcp.Server.exe diff --pid 1234 --before before --after after
WpfVisualTreeMcp.Server.exe revert-property --pid 1234 --all
```

## 与 MCP 的对应关系

几乎每个 `[McpServerTool]` 都有同名能力的 CLI 子命令（命名多为短横线形式，如 `wpf_find_elements` → `find`）。Agent 既可通过 MCP 调用，也可在终端直接跑 CLI；句柄规则与状态变更语义一致。

更多参数细节见 [工具参考](TOOLS_REFERENCE.md)。
