# WinUIIslands.Windowing

这个目录下的 API 仿照 [WinUI 3](https://learn.microsoft.com/windows/apps/winui/winui3/) 的 `Microsoft.UI.Windowing` 命名空间设计，提供一致的窗口管理体验。

## API 对应关系

| WinUIIslands.Windowing | Microsoft.UI.Windowing |
|---|---|
| `AppWindow` | `Microsoft.UI.Windowing.AppWindow` |
| `AppWindowPresenter` | `Microsoft.UI.Windowing.AppWindowPresenter` |
| `AppWindowPresenterKind` | `Microsoft.UI.Windowing.AppWindowPresenterKind` |
| `OverlappedPresenter` | `Microsoft.UI.Windowing.OverlappedPresenter` |
| `WindowNative` | `Microsoft.UI.Xaml.WindowNative` |
| `Win32Interop` | `Microsoft.UI.Win32Interop` |
| `WindowId` | `Microsoft.UI.WindowId` |
| `SizeInt32` | `Windows.Graphics.SizeInt32` |

## 使用方式

```csharp
using WinUIIslands;
using WinUIIslands.Windowing;

var window = new Window();
window.Activate();

var hWnd = WindowNative.GetWindowHandle(window);
WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

if (appWindow.Presenter is OverlappedPresenter presenter)
{
    presenter.IsResizable = false;
    presenter.IsMaximizable = false;
    presenter.PreferredMinimumWidth = 640;
    presenter.PreferredMinimumHeight = 500;
}

appWindow.Resize(new SizeInt32(800, 600));
```

也可以直接从 `Window` 获取 `AppWindow`：

```csharp
if (window.AppWindow?.Presenter is OverlappedPresenter presenter)
{
    presenter.IsResizable = false;
}
```
