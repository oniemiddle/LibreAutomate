# LibreAutomate 迁移到 Avalonia 盘点（现状 + 策略）

## 0. 目标与边界

本文用于给出 **从当前 WPF/WinForms + Windows 专用栈** 迁移到 **Avalonia UI** 的第一轮盘点，聚焦：

- 分层现状（UI / 应用 / 领域 / 基础设施）。
- 每个模块的迁移策略（直接复用 / 包装适配 / 完全重写）。
- 风险矩阵与阻断项。
- 最小可用版本（MVP）范围与后续阶段切分。

---

## 1. 按模块现状盘点

## 1.1 UI 层（窗口、控件、主题、资源字典）

### 现状清单

| 子模块 | 现有实现线索 | 现状判断 | 迁移策略 |
|---|---|---|---|
| 主窗口与应用资源 | `Au.Editor/App/MainWindow.cs`、`Au.Editor/App/App-resources.xaml` | 主编辑器与全局资源依赖 WPF 资源体系。 | **完全重写**（视图层重建为 Avalonia XAML + ViewModel） |
| 自定义控件库 | `Au.Controls/*`（KTreeView/KPanels/KScintilla/Simple） | 大量自绘、WPF 控件扩展、Wnd/Win32 互操作。 | **包装适配 + 局部重写**（优先保留行为接口，替换控件实现） |
| 编辑器控件链路 | `Au.Editor/Edit/*` + `Libraries/scintilla` | 编辑器深度绑定 Scintilla 与现有宿主封装。 | **包装适配**（先抽象 `ITextEditorHost`，后替换 UI 宿主） |
| 主题与样式资源 | `Au.Editor/Default/Themes/*.csv`、`App-resources.xaml`、`Au.Controls/resources/Generic.xaml` | 主题体系是“CSV 配色 + XAML 资源”混合模型。 | **包装适配**（保留主题数据，重写资源映射） |
| 托盘/菜单/弹窗 | `Au.Editor/App/App.TrayIcon.cs`、`Menus.cs`、多种对话框与弹窗 | 依赖 Windows Shell 与 WPF 菜单/窗口行为。 | **完全重写**（UI 与平台 API 解耦） |

### UI 层结论

- **高改造密度**：窗口树、资源字典、控件模板几乎都需要迁移。
- **可复用资产**：主题数据、控件交互规则、编辑器业务行为（非视图代码）可复用。

---

## 1.2 应用层（命令、流程编排、状态管理）

### 现状清单

| 子模块 | 现有实现线索 | 现状判断 | 迁移策略 |
|---|---|---|---|
| 启动与生命周期 | `Au.Editor/App/App.cs`、`CommandLine.cs` | 启动流程与命令行逻辑清晰，但与 WPF App 生命周期耦合。 | **包装适配**（抽象 `IAppLifecycle`） |
| 命令分发与菜单动作 | `Menus.cs`、`Compiler/Run task.cs`、`Panels/*` 入口调用 | 当前命令多由 UI 事件直接触发业务。 | **包装适配**（命令总线化，逐步 MVVM 化） |
| 工作区与状态 | `Files/WorkspaceState.cs`、`FilesModel.cs`、`AppSettings.cs` | 状态对象可迁移，状态变更通知机制需统一。 | **直接复用 + 包装适配** |
| 任务运行与调度 | `Compiler/Compiler.cs`、`Run task.cs`、`Debugger/*` | 运行链路价值高，UI 反馈通道需重构。 | **包装适配** |

### 应用层结论

- 应用层总体可迁移性较好；主要问题在于“UI 事件直达业务”。
- 建议先拆出 `Application Services`，让 Avalonia 前端仅调用接口。

---

## 1.3 领域层（模型、规则、任务定义）

### 现状清单

| 子模块 | 现有实现线索 | 现状判断 | 迁移策略 |
|---|---|---|---|
| 类型与通用模型 | `Au/Au.Types/*` | 多数是 UI 无关的数据与类型逻辑。 | **直接复用** |
| 脚本/任务元信息 | `Au/Other/script*.cs`、`Compiler/Meta*.cs` | 规则与元数据逻辑可独立存在。 | **直接复用** |
| 规则与工具函数 | `Au/String/*`、`Au/Ext/*` | 绝大多数是纯逻辑工具，无 UI 绑定。 | **直接复用** |
| 与 Win32 强绑定的“领域行为” | `Au/wnd/*`、`Au/Api/*` 被上层直接调用 | 若作为“领域规则”被调用，会污染跨平台层。 | **包装适配**（下沉到平台服务接口） |

### 领域层结论

- 领域核心可作为迁移“稳定锚点”。
- 要避免把 Win32 行为继续暴露到 ViewModel/应用服务。

---

## 1.4 基础设施层（IO、进程、脚本执行、系统集成）

### 现状清单

| 子模块 | 现有实现线索 | 现状判断 | 迁移策略 |
|---|---|---|---|
| 文件与目录 IO | `Au/Au.More/*`、`Au/Other/*`、`Files/SyncWithFilesystem.cs` | 逻辑可保留，但路径/编码/监控细节需验证。 | **直接复用 + 包装适配** |
| 进程与宿主 | `Au.Internal/ProcessStarter_.cs`、`Au.AppHost/*`、`Other/Au.DllHost/*` | 与现有宿主/注入机制耦合深。 | **包装适配** |
| 脚本编译执行 | `Compiler/*`、Roslyn 引用 | 编译执行是核心能力，技术栈本身可沿用。 | **直接复用** |
| 系统集成（Win32/UIA/COM） | `Au/Api/*`、`Au/wnd/*`、`Au/Au.More/WindowsHook.cs` | 平台能力高度 Windows 专用。 | **包装适配（Windows 目标）/ 完全替换（跨平台目标）** |
| 第三方依赖 | WebView2、Scintilla、MSTSCLib、AxMSTSCLib | 存在 UI 技术栈或平台绑定。 | **按依赖逐项替换或隔离** |

### 基础设施层结论

- 如果目标是“先 Windows 上线”，可保留大部分 Win32 基础设施并通过接口隔离。
- 如果目标是“跨平台”，系统集成能力需要分级（可用 / 降级 / 不支持）。

---

## 2. 迁移策略总览（按优先级）

1. **先抽象接口，再迁 UI**：
   - `IEditorShell`、`ICommandDispatcher`、`ITrayService`、`INotificationService`、`IPlatformAutomationService`。
2. **双壳并行阶段**：
   - 旧 WPF 壳继续可用；新 Avalonia 壳逐步接管核心流程。
3. **先保核心路径，再补功能**：
   - 先实现编辑-运行-查看输出闭环，再迁调试、录制、远程控件等高级功能。

---

## 3. 风险矩阵与阻断项

## 3.1 风险矩阵

| 风险项 | 等级 | 影响 | 缓解措施 |
|---|---|---|---|
| WPF 自定义控件迁移成本（KTreeView/KPanels/KDialog 等） | 高 | 影响主界面可用性与开发周期 | 先接口化行为，再按页面逐步替换 |
| Scintilla 宿主与输入法/快捷键兼容 | 高 | 影响核心编辑体验 | 保持原编辑内核，先迁外层壳，再处理细节 |
| Win32/COM/UIA 与 UI 线程模型差异 | 高 | 易出现死锁、焦点异常、事件丢失 | 统一调度器抽象，隔离 UI 线程与系统调用线程 |
| 菜单/托盘/通知在不同平台行为差异 | 中 | 影响日常操作与用户习惯 | 先 Windows 等效实现，跨平台再做降级策略 |
| 主题系统（CSV + XAML）映射不一致 | 中 | 影响视觉一致性 | 建立中间主题模型（Token）统一映射 |
| 构建链路（C++ 组件 + 多项目）复杂 | 中 | CI 与发布成本上升 | 增加分层构建脚本与最小构建目标 |
| 文档与插件生态暂未同步 | 低 | 上手成本增加 | 在 MVP 后补齐文档与迁移指南 |

## 3.2 阻断项（需要先解决）

1. **依赖库仅支持旧 UI 栈或 Windows 控件宿主**（如部分 ActiveX/COM UI 控件）。
2. **线程模型不兼容**：现有代码假设 WPF Dispatcher/Win32 消息循环语义。
3. **控件替代缺口**：复杂 Dock/Tree/Code Editor 组合在 Avalonia 中需要额外适配层。
4. **系统托盘与全局热键实现差异**：跨平台行为不一致，需定义“能力矩阵”。
5. **测试基线缺失**：缺少可自动对比的 UI 回归基准会放大迁移风险。

---

## 4. MVP（最小可用版本）范围

## 4.1 MVP 必须包含（核心用户路径）

> 目标：保证“日常写脚本并运行验证”可闭环。

1. **项目/脚本基本管理**
   - 打开工作区、浏览文件树、打开/保存脚本。
2. **代码编辑核心能力**
   - 基础高亮、自动补全（可先降级）、查找替换、基础格式化。
3. **运行与输出闭环**
   - 运行当前脚本、查看输出/错误、停止任务。
4. **基础设置与主题**
   - 至少 1 套默认主题 + 字体/字号等关键偏好。
5. **Windows 平台下关键自动化能力可调用**
   - 通过接口桥接现有 `Au` 能力，不要求跨平台全量一致。

## 4.2 明确排除到后续阶段（非核心）

- 完整调试器高级特性（断点高级条件、复杂可视化）。
- 全量工具面板（录制器、对象抓取器、数据库/图标辅助工具等）。
- 远程桌面集成、WebView2 相关高级功能。
- 插件/扩展生态与全部命令自定义迁移。
- 跨平台一致体验（Linux/macOS）与平台特化能力对齐。

---

## 5. 建议的阶段划分（执行参考）

- **Phase 0（2~4 周）**：接口分层与“可编译双壳”骨架。
- **Phase 1（4~8 周）**：MVP 核心路径上线（编辑-运行-输出）。
- **Phase 2（4~8 周）**：补齐高频面板、设置、主题与稳定性。
- **Phase 3（持续）**：高级工具链与跨平台能力矩阵扩展。

> 里程碑验收建议：每阶段必须可演示“真实脚本从编辑到运行”的端到端流程。

---

## 6. 新解决方案结构与工程边界（建议落地版）

为避免“先迁 UI、后补分层”导致返工，建议在 Phase 0 即完成如下解决方案结构：

- `LibreAutomate.App`
  - 职责：Avalonia 启动入口、DI 组合根、平台生命周期接入。
  - 只做装配（Composition Root），不承载业务规则。
- `LibreAutomate.Presentation`
  - 职责：View / ViewModel、交互状态、页面路由、UI 级校验。
  - 依赖 `Application` 暴露的用例接口，不直接访问 `Infrastructure`。
- `LibreAutomate.Application`
  - 职责：用例编排、命令处理、事务边界、服务接口定义（Ports）。
  - 只依赖 `Domain` 抽象，不依赖具体 UI 技术。
- `LibreAutomate.Domain`
  - 职责：核心模型、领域规则、不可变值对象、领域服务。
  - 不依赖任何外层项目（纯净内核）。
- `LibreAutomate.Infrastructure`
  - 职责：文件系统、进程、系统 API、第三方集成实现。
  - 通过实现 `Application` 定义的接口接入，不反向依赖 `Presentation`。

推荐同时建立测试项目：

- `LibreAutomate.Application.Tests`
- `LibreAutomate.Domain.Tests`
- `LibreAutomate.Infrastructure.Tests`（集成/契约测试为主）

## 7. 依赖方向与引用规则（强约束）

统一依赖方向：`Presentation -> Application -> Domain`。

`Infrastructure` 作为外部实现层，仅依赖 `Application`（接口）与必要的 `Domain` 类型，不允许被 `Presentation` 直接引用。建议在 CI 增加架构测试（例如 NetArchTest）保证以下规则：

1. `Domain` 不引用任何 `Presentation/Application/Infrastructure` 命名空间。
2. `Application` 不引用 `Presentation/Infrastructure` 实现命名空间。
3. `Presentation` 不引用 `Infrastructure`。
4. `App` 允许引用 `Presentation` 与 `Infrastructure`，但仅用于依赖注入装配。

简化示意：

```text
LibreAutomate.App
 ├─> LibreAutomate.Presentation ─> LibreAutomate.Application ─> LibreAutomate.Domain
 └─> LibreAutomate.Infrastructure ───────────────────────────────┘(实现 Application Ports)
```

## 8. ViewModel、命令与事件通知规范

为降低后续维护成本，建议从第一天统一 MVVM 基础设施。

### 8.1 ViewModel 基类

- 统一 `ViewModelBase : ObservableObject`（可基于 CommunityToolkit.Mvvm）。
- 所有可绑定状态必须通过受控属性暴露，禁止公共字段。
- 在 `ViewModelBase` 内置：
  - `IsBusy` / `BusyMessage`（统一忙碌态）；
  - `CancellationTokenSource? CurrentCts`（统一取消入口）；
  - `SetProperty` 与可选的 `OnPropertyChanged(string?)` 扩展钩子。

### 8.2 命令机制

- 同步操作：`RelayCommand`。
- 异步操作：`AsyncRelayCommand`，必须支持取消（`CancellationToken`）。
- 长任务命令约定：
  1. 执行前设置 `IsBusy = true`；
  2. 使用 `try/finally` 保证结束时恢复 `IsBusy = false`；
  3. 对用户可见失败统一经 `IUserNotificationService` 输出，而非静默吞错。

### 8.3 事件通知规范

- 属性变更仅通过 `INotifyPropertyChanged`，避免自定义事件泛滥。
- 跨 ViewModel/模块通信优先使用消息总线（如 `IMessenger`）或应用事件接口，避免直接互相持有引用。
- 领域事件不直接进入 UI；应先在 `Application` 层转换为展示模型/通知模型。

## 9. 主题、样式与资源字典约定

为防止后期样式碎片化，建议在 `LibreAutomate.Presentation` 固定以下目录结构：

```text
Presentation/
  Themes/
    Tokens/                # 设计 Token（颜色/间距/圆角/字体）
      ColorTokens.axaml
      SpacingTokens.axaml
      TypographyTokens.axaml
    Variants/              # 主题变体
      Theme.Light.axaml
      Theme.Dark.axaml
    Controls/              # 控件级样式
      ButtonStyles.axaml
      TreeViewStyles.axaml
      EditorStyles.axaml
    AppTheme.axaml         # 全局合并入口（唯一入口）
```

命名约定：

- 资源键使用前缀分组：
  - 颜色：`Color.*`（如 `Color.Surface.Primary`）
  - 间距：`Space.*`（如 `Space.200`）
  - 字体：`Font.*`（如 `Font.Size.Body`）
  - 控件样式：`Style.*`（如 `Style.Button.Primary`）
- 禁止在页面内写“匿名硬编码色值”；必须引用 Token。
- 主题切换只在 `AppTheme.axaml` 层执行，页面不直接切换字典。

建议把现有“CSV 配色 + XAML 资源”映射为统一 Token 中间层：

1. 先把 CSV 映射到 `Color.*` Token。
2. 控件样式仅消费 Token，不直接读 CSV 字段。
3. 后续替换主题来源时，控制在 Token 层，避免控件样式大面积改动。
