# CODEBUDDY.md This file provides guidance to CodeBuddy when working with code in this repository.

## 项目概述

WPF 桌面应用（屏幕录制工具），基于 .NET 8（`net8.0-windows`），采用纯手写 MVVM 架构，不依赖任何第三方 MVVM 框架（如 Prism、CommunityToolkit.Mvvm）。

## 常用命令

```powershell
# 编译
dotnet build ScreenRecordingTool.sln

# 运行
dotnet run --project ScreenRecordingTool/ScreenRecordingTool.csproj

# 发布
dotnet publish ScreenRecordingTool/ScreenRecordingTool.csproj -c Release
```

当前项目没有测试工程；如新增测试，使用 `dotnet test` 运行。

## 架构

### MVVM 分层（ScreenRecordingTool/ 目录下）

- **`Mvvm/`** — 手写 MVVM 基础设施，所有新代码必须复用这里的基类，禁止引入第三方 MVVM 框架：
  - `ViewModelBase`：实现 `INotifyPropertyChanged`，提供 `SetProperty<T>(ref field, value)` 与 `OnPropertyChanged()`。
  - `RelayCommand` / `RelayCommand<T>`：同步命令，`CanExecuteChanged` 挂接 `CommandManager.RequerySuggested`，可执行状态自动刷新。
  - `AsyncRelayCommand`：异步命令，执行期间自动禁用以防重复触发。
- **`Models/`** — 纯数据模型（POCO），不含界面逻辑，不引用 WPF 类型。
- **`ViewModels/`** — 继承 `ViewModelBase`，持有状态与命令。禁止直接引用 View 控件或 `System.Windows.Controls` 类型。
- **`Views/`** — XAML 窗口/控件。代码隐藏（.xaml.cs）保持最小化，仅处理与视图强相关的逻辑，所有交互通过 `{Binding}` 走 ViewModel。

### 启动与装配方式

`App.xaml` **不使用** `StartupUri`。`App.xaml.cs` 的 `OnStartup` 是组合根（Composition Root）：在此创建 `MainWindow` 并注入 `MainViewModel` 作为 `DataContext`。新增窗口/服务的装配也应在此完成。

### 关键约定

- View 与 ViewModel 通过 `DataContext` 外部注入关联，而不是在 View 内部 `new` ViewModel。
- XAML 中使用 `d:DataContext="{d:DesignInstance ...}"` 提供设计时绑定智能提示。
- 录屏等业务逻辑应放入独立的 Services 层（通过接口注入 ViewModel），ViewModel 中的 `// TODO` 标注了预留的接入点。

## 录屏功能相关命令
### 开始录制
$pipe = New-Object System.IO.Pipes.NamedPipeClientStream(".", "ScreenRecordingTool.Pipe", [System.IO.Pipes.PipeDirection]::Out)
$pipe.Connect(3000)
$writer = New-Object System.IO.StreamWriter($pipe)
$writer.WriteLine("START TaskA2")
$writer.Flush()
$pipe.Dispose()



### 停止录制（新连接）
$pipe = New-Object System.IO.Pipes.NamedPipeClientStream(".", "ScreenRecordingTool.Pipe", [System.IO.Pipes.PipeDirection]::Out)
$pipe.Connect(3000)
$writer = New-Object System.IO.StreamWriter($pipe)
$writer.WriteLine("STOP TaskA2")
$writer.Flush()
$pipe.Dispose()

