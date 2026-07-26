# 工具参考

WpfVisualTreeMcp 提供的全部 MCP 工具说明。CLI 子命令与之对应，见 [CLI 参考](CLI.md)。

**约定：** 标有「会改状态」的工具会改变目标应用 UI/状态。元素句柄来自 `wpf_find_elements` 等，格式为 `elem_XXXXXXXX`。

---

## 进程管理

### wpf_list_processes

列出可检查的运行中 WPF 应用。

**参数：** 无

**返回字段示例：** `process_id`、`process_name`、`main_window_title`、`is_attached`、`dotnet_version`、`runtime_type`

---

### wpf_attach

按进程 ID 或名称附加。

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `process_id` | int | 二选一 | 进程 ID |
| `process_name` | string | 二选一 | 进程名 |
| `auto_inject` | bool | 否 | `true` 时向未预载 Inspector 的进程自动注入 |

**注意：** 返回的 `main_window_handle`（`window_0x…`）是 Win32 HWND，**不要**当作其他工具的 `element_handle`。整窗操作省略 `element_handle`；控件请用 `elem_…`。

---

## 树导航与查询

### wpf_get_visual_tree

获取可视化树。

| 参数 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `root_handle` | string | 主窗口 | 子树根 |
| `max_depth` | int | 25 | 深度 1–100 |

深 UI（如 AvalonDock）请加大 `max_depth`，或用 `root_handle` 聚焦子树。

---

### wpf_find_elements

跨所有打开窗口查询元素（AND 过滤）。

| 参数 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `root_handle` | string | — | 限定范围 |
| `type_name` | string | — | 类型部分匹配（如 `Button`） |
| `element_name` | string | — | x:Name 子串 |
| `text` | string | — | 可见文本（按钮文案、TextBlock、窗口标题、ToolTip、Automation Name 等） |
| `property_filter` | object | — | 属性名 → 值子串，如 `{"IsEnabled":"False"}` |
| `visible_only` | bool | false | 排除折叠/隐藏 |
| `max_results` | int | 50 | 结果上限 |

**结果含：** `text`、`automationId`、`isVisible`、`isEnabled`、`screenBounds`（设备像素）。

优先用 `text` + `type_name`，避免整树倾倒。

---

### wpf_find_elements_deep

同过滤条件，**无结果上限**。至少提供 `type_name`、`element_name` 或 `text` 之一（防止返回整棵树）。

---

## 属性、布局与数据

### wpf_get_element_properties

读取元素全部依赖属性。

| 参数 | 必填 | 说明 |
|------|------|------|
| `element_handle` | 是 | 元素句柄 |

属性来源常见值：`Default`、`Local`、`Style`、`Template`、`Inherited`、`Animation` 等。

---

### wpf_get_layout_info

布局信息：ActualWidth/Height、DesiredSize、RenderSize、Margin、Padding、对齐、Visibility 等。

---

### wpf_get_bindings

元素上的数据绑定（含 MultiBinding、Converter、StringFormat 等）。

---

### wpf_get_binding_errors / wpf_clear_binding_errors

列出自应用启动以来通过 WPF 跟踪监听捕获的绑定错误；清空列表便于针对性复现。

---

### wpf_get_data_context

DataContext 类型、属性、是否 INPC、沿可视化树的继承链。诊断绑定路径错误时优先使用。

---

### wpf_get_resources

| 参数 | 默认 | 说明 |
|------|------|------|
| `scope` | `application` | `application` / `window` / `element` |
| `element_handle` | — | `scope=element` 时必填 |

---

### wpf_get_styles

已应用样式与模板摘要。

---

### wpf_watch_property

注册监视某属性变化。当前 IPC 仍以请求/响应为主，变更推送能力仍在演进中。

| 参数 | 必填 |
|------|------|
| `element_handle` | 是 |
| `property_name` | 是 |

---

## 截图与高亮

### wpf_capture_screenshot

| 参数 | 默认 | 说明 |
|------|------|------|
| `element_handle` | 主窗口 | 用 `elem_…`；勿传 `window_0x…` |
| `max_width` / `max_height` | 1920 / 1080 | 过大则缩小 |
| `mode` | `render` | `render`：离屏重绘，遮挡也可；看不到 Popup。`screen`：屏幕像素，含 Popup/下拉/菜单，窗口须可见 |

返回 MCP 图像内容，Agent 可直接「看见」。

---

### wpf_highlight_element

| 参数 | 默认 | 说明 |
|------|------|------|
| `element_handle` | — | 必填 |
| `duration_ms` | 2000 | 高亮时长 |

---

## 等待、快照与 Diff

### wpf_wait_for_element

在目标进程内轮询，直到匹配元素满足条件（无需 sleep 重试）。CLI 子命令为 `wait-for`。

| 参数 | 默认 | 说明 |
|------|------|------|
| `type_name` / `element_name` / `text` | — | 至少一项 |
| `condition` | `visible` | `visible` / `exists` / `enabled` / `hidden` |
| `timeout_ms` | 10000 | 最大约 25000 |
| `poll_interval_ms` | 250 | 轮询间隔 |
| `root_handle` | — | 限定范围 |

返回：`matched`、`waited_ms`、`matched_handle`、`element_type`。

---

### wpf_snapshot

捕获子树精选状态（可见性、IsEnabled、尺寸、Margin、画刷、文本等），按标签存储。

| 参数 | 默认 | 说明 |
|------|------|------|
| `element_handle` | 主窗口 | 子树根 |
| `label` | 自动 | 标签名 |
| `max_depth` | 25 | 深度 |

---

### wpf_diff

比较两个快照标签，报告属性 from→to、新增/移除元素（句柄对齐同一元素）。

| 参数 | 必填 |
|------|------|
| `before` | 是 |
| `after` | 是 |

典型闭环：`snapshot(before)` → `set_property` / 点击 → `snapshot(after)` → `diff`。

---

## 会改变状态的交互

### wpf_click_element

| 参数 | 默认 | 说明 |
|------|------|------|
| `element_handle` | — | 必填 |
| `physical` | false | `true`：真实 OS 鼠标点击（会移光标、前置窗口；先滚入视图） |
| `click_type` | `single` | `single` / `double` / `right`；双击与右键**始终**物理点击 |

默认走 UI Automation（Invoke / Toggle / Select / ExpandCollapse）。打开菜单后截图请用 `mode=screen`。

---

### wpf_select_item

在 ComboBox / ListBox / ListView / TabControl 中按可见文本或索引选中；支持虚拟化项。失败时错误信息常带可用项列表。

| 参数 | 说明 |
|------|------|
| `element_handle` | 必填 |
| `item_text` | 与 `index` 二选一（文本不区分大小写子串） |
| `index` | 从 0 开始 |

下拉/列表优先用本工具，而不是盲目点击。

---

### wpf_set_text

设置 TextBox / ComboBox / PasswordBox 等文本。

| 参数 | 默认 | 说明 |
|------|------|------|
| `element_handle` | — | 必填 |
| `text` | — | 要写入的文本 |
| `physical` | false | `true`：聚焦后用 OS 键盘输入（先 Ctrl+A/Delete） |

默认 `IValueProvider.SetValue`，失败则回退到直接属性 / 反射。响应含写回读验证。

---

### wpf_send_keys

发送快捷键到元素或当前焦点窗口。

| 参数 | 说明 |
|------|------|
| `keys` | 必填，如 `Ctrl+S`、`Alt+F4`、`F5`、`Enter` |
| `element_handle` | 可选；省略则发到当前焦点 |

修饰键：Ctrl / Shift / Alt / Win。键：字母数字、F1–F12、Enter、Esc、Tab、方向键等。使用 OS 输入并前置窗口。

---

### wpf_set_property

运行时改写依赖属性（字符串经 TypeConverter 转换），用于不重建就试改布局/样式。

| 参数 | 说明 |
|------|------|
| `element_handle` | 必填 |
| `property_name` | 如 `Margin`、`Visibility`、`Background`、`Width` |
| `value` | 如 `20,0,20,0`、`Collapsed`、`Red`、`#FF0000`、`300`；`{null}` 表示 null |

覆盖数据绑定时变为本地值；用 `wpf_revert_property` 可恢复绑定。只读 DP 会报错。

---

### wpf_revert_property

撤销 `wpf_set_property`。

| 参数 | 说明 |
|------|------|
| `element_handle` / `property_name` | 可选过滤 |
| `all` | `true` 撤销会话内全部待回滚编辑 |

默认撤销最近一次。返回 `reverted_count`、`pending_count` 等。

---

## 导出

### wpf_export_tree

| 参数 | 默认 | 说明 |
|------|------|------|
| `element_handle` | 主窗口 | 根 |
| `format` | `json` | `json` 或 `xaml` |

---

## 推荐工作流

1. `wpf_list_processes` → `wpf_attach`（需要时 `auto_inject=true`）
2. `wpf_find_elements(text=…, type_name=…)` 定位，必要时 `wpf_highlight_element`
3. `wpf_get_binding_errors` / `wpf_get_data_context` / `wpf_get_bindings` 诊断
4. 交互：`wpf_set_text` / `wpf_select_item` / `wpf_click_element` → `wpf_wait_for_element` → `wpf_capture_screenshot`
5. 试改：`wpf_snapshot` → `wpf_set_property` → `wpf_snapshot` → `wpf_diff` → `wpf_revert_property`
