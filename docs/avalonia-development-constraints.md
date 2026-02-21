# Avalonia 开发约束说明

## 1. 命名规范
- **类型命名**：类、接口、枚举使用 PascalCase；接口前缀 `I`。
- **字段命名**：私有字段使用 `_camelCase`。
- **View / ViewModel 对应**：`XxxView.axaml` 对应 `XxxViewModel.cs`，命名必须一一映射。
- **配置项命名**：配置模型属性使用 PascalCase，JSON 键与属性同名，避免魔法字符串。

## 2. 绑定规范
- 默认使用 `OneWay` 绑定，只有交互输入才使用 `TwoWay`。
- 页面切换统一绑定到 `MainWindowViewModel.CurrentPage`，由 DataTemplate 解析具体视图。
- 不在 code-behind 操作业务状态；code-behind 仅用于 UI 生命周期和控件初始化。

## 3. 命令规范
- 所有按钮动作通过 `ICommand` 暴露，不在 XAML 里绑定事件处理方法。
- 同步动作使用 `DelegateCommand`；耗时动作应扩展为异步命令并支持取消。
- 命令是否可用由 `CanExecute` 控制，功能开关由配置项驱动（如 `FeatureFlags`）。

## 4. 异步规范
- I/O、网络、文件系统、插件加载等操作必须异步化，避免阻塞 UI 线程。
- 异步方法必须返回 `Task` / `Task<T>`，禁止 `async void`（事件处理除外）。
- 后台任务异常必须记录到统一日志并纳入全局异常处理。

## 5. 线程切换规范
- ViewModel 默认在后台线程执行任务；涉及 UI 状态更新时切回 UI 线程。
- Avalonia UI 线程切换使用 `Dispatcher.UIThread.Post/InvokeAsync`。
- 禁止从后台线程直接访问控件实例，统一通过可绑定属性更新 UI。

## 6. 可诊断性要求
- 启动阶段必须初始化：配置、日志、DI 容器、全局异常处理。
- 发生不可恢复启动异常时，必须输出控制台错误并显示降级提示。
- 关键导航与模块加载必须记录结构化日志，至少包含目标页面/模块名。
