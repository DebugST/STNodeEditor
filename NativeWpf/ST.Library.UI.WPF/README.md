# ST.Library.UI.WPF

Native WPF controls for STNodeEditor. The editor is a WPF `Control`; it does
not host a WinForms control and has no third-party rendering dependency.

This project is intentionally separate from `ST.Library.UI`: the existing project remains the WinForms implementation, while WPF applications reference this assembly instead. Both assemblies expose the `ST.Library.UI.NodeEditor` namespace so existing node source can keep the same node, option, drawing, and serialization contracts.

A consumer must reference only one platform implementation in a single application.

## Usage

Reference `ST.Library.UI.WPF.csproj` from a WPF application and place the
controls directly in XAML:

```xml
<Window xmlns:st="clr-namespace:ST.Library.UI.NodeEditor;assembly=ST.Library.UI.WPF">
    <Grid>
        <st:STNodeEditor />
    </Grid>
</Window>
```

`STNodeEditor`, `STNodeTreeView`, `STNodePropertyGrid`, and the historical
`STNodeEditorPannel` type are all native WPF elements. Custom node drawing
continues to use the original `DrawingTools.Graphics` contract. The WPF
control copies that GDI-rendered surface into a DPI-aware `WriteableBitmap`.

Custom property descriptors keep the upstream drawing and interaction model.
Derived descriptors render through a native WPF presenter, receive their logical
property-grid rectangles, and receive neutral mouse enter/move/leave/down/up/
click events. Default editing is provided by native WPF text and selection
controls.

Mouse callbacks use `STNodeMouseEventArgs` and `STMouseButtons` so the WPF
assembly has no dependency on WinForms assemblies. Node construction keeps
the upstream lifecycle: `OnCreate` runs once during construction, and repeated
compatibility calls to `Create()` are safe and idempotent.
