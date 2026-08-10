# Native WPF implementation

This directory is self-contained. It adds a native WPF implementation without changing the repository's existing WinForms library, WinForms demo, legacy WPF host demo, or original solution.

## Projects

- `ST.Library.UI.WPF`: dependency-free native WPF controls rendered through the original `System.Drawing.Graphics` contract and presented by a DPI-aware WPF `WriteableBitmap`.
- `WpfNodeEditorDemo`: full-window node canvas with context menus, automatic layout, save/open actions, an execution command, and a node-anchored reflection property panel provided by the `ColorVision.UI` 1.5.7 NuGet package.
- `STNodeEditor.Wpf.sln`: standalone solution for the two projects above.

The control library has no NuGet dependency and does not reference `System.Windows.Forms`, `WindowsFormsIntegration`, or a third-party rendering package. The demo uses a native WPF visual tree with no `WindowsFormsHost`; only the demo references `ColorVision.UI` for its reflection-based property editors and theme resources.

Node creation is available from the integrated `+` button and the canvas context menu, so the demo does not reserve space for a node tree. Selecting one node opens its reflected `STNodePropertyAttribute` properties beside the node while leaving the rest of the window available to the canvas.

The demo uses direct cursor-centered wheel zoom in `0.05` steps across the editor's full `0.2` to `5.0` scale range. The canvas lock button controls blank-area left-drag explicitly: unlocked pans the canvas, locked draws a selection rectangle, and middle-button drag always pans. Manual lock state is not changed by clicking a node.

The demo keeps the historical `WpfNodeEdittorDemo` assembly name so existing STND files retain the same node module identity; only the project folder and UI namespace use the corrected spelling.

## Run

Open `STNodeEditor.Wpf.sln`, select `WpfNodeEditorDemo`, and press F5. From PowerShell, the equivalent commands are:

```powershell
dotnet build .\STNodeEditor.Wpf.sln
dotnet run --project .\WpfNodeEditorDemo\WpfNodeEditorDemo.csproj
```

The WPF library targets both `net8.0-windows` and `net10.0-windows`; the demo targets `net8.0-windows` and runs as x64 because the published `ColorVision.UI` assemblies are AMD64. This does not alter the target framework or source of any existing project.
