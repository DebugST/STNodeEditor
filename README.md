# STNodeEditor

STNodeEditor 是一个轻量且功能强大的节点编辑器 使用方式非常简洁 提供了丰富的属性以及事件可以非常方便的完成节点之间数据的交互及通知 大量的重载函数供开发者使用具有很高的自由性

![STNodeEditor](https://debugst.github.io/DotNet_WinForm_NodeEditor/images/page_top.png)

项目主页 (Project home):  [DebugST.github.io/DotNet_WinForm_NodeEditor](https://DebugST.github.io/DotNet_WinForm_NodeEditor) (简体中文, English)

教程文档: [DebugST.github.io/DotNet_WinForm_NodeEditor/doc_cn.html](https://DebugST.github.io/DotNet_WinForm_NodeEditor/doc_cn.html)

Tutorials and API: [DebugST.github.io/DotNet_WinForm_NodeEditor/doc_en.html](https://DebugST.github.io/DotNet_WinForm_NodeEditor/doc_en.html)

Mail: (2212233137@qq.com)

# STNodeEditor

![STNodeEditor](https://debugst.github.io/DotNet_WinForm_NodeEditor/images/stnodeeditor.gif)

`STNodeEditor`拥有非常强大的功能 支持画布的移动和缩放 可以对节点位置以及连线进行锁定 连线时候会自动检测数据类型是否兼容 以及连线是否重复或者构成环形线路等问题

* 拖动标题移动节点
* 右击标题弹出菜单 (需要设置`ContextMenuStrip`)
* 拖动连接点进行连线
* 右击连线断开连接
* 中键拖动移动画布 (若笔记本触摸板支持 可二指拖动)
* CTRL+鼠标滚轮 缩放画布

__注:节点Body区域进行的操作编辑器不会响应 因为在节点客户区内部的操作将被转换为节点的事件__

__因为作者将一个节点视为一个`Form` 而编辑器容器则为`Desktop` 开发者可以像开发`WinForm`程序一样去开发一个节点__

# STNodeHub

![STNodeHub](https://debugst.github.io/DotNet_WinForm_NodeEditor/images/stnodehub.gif)

`STNodeHub`是一个内置的节点 其主要作用分线 可以将一个输出分散到多个输入或多个输出集中到一个输入点上以避免重复布线 也可在节点布线复杂时用于绕线

HUB的输入输出默认为`object`类型 当一个连接被连入时候将会自动更换数据类型并增加新行

__注:仅`STNodeHub`可以修改连接点的数据类型 因为相应字段被`internal`标记 而作为第三方扩展的STNode中是无法修改已添加连接点的数据类型的__

# STNodeTreeView

![STNodeTreeView](https://debugst.github.io/DotNet_WinForm_NodeEditor/images/stnodetreeview.gif)

`STNodeTreeView`可与`STNodeEditor`结合使用`STNodeTreeView`中的节点可直接拖拽进`STNodeEditor`中 并且提供预览和检索功能

`STNodeTreeView`的使用简单 无需像`System.Windows.Forms.TreeView`需要自行去构造树

通过使用`STNodeAttribute`标记继承的`STNode`可直接设置需要在`STNodeTreeView`中显示的路径 以及希望在`STNodePropertyGrid`中显示的信息

__注:若希望节点能够在`STNodeTreeView`中显示 必须使用`STNodeAttribute`标记`STNode`子类__

# STNodePropertyGrid

![STNodePropertyGrid](https://debugst.github.io/DotNet_WinForm_NodeEditor/images/stnodepropertygrid.gif)

若`STNode`中的属性被`STNodePropertyAttribute`标记则会在`STNodePropertyGrid`中显示 默认情况下支持`int,float,double,bool,string,enum`以及上述数据类型的`Array` 若希望显示的属性数据类型不被支持 可以对`DescriptorType`进行扩展重写 详细请参考DEMO

可以看到在`STNodePropertyGrid`的面板中可以显示节点的一些信息 作者认为提供给大家的是一套框架 大家可以基于这套框架打造一套自己的框架 

__而为框架编写节点的`Coder`应该有权利选择是否留下个人信息__

# STNodeEditorPannel

![STNodeEditorPannel](https://debugst.github.io/DotNet_WinForm_NodeEditor/images/stnodeeditorpannel.gif)

`STNodeEditorPannel`是`STNodeEditor` `STNodeTreeView` `STNodePropertyGrid`的一套组合控件

可以通过拖动手柄控制布局 

# 关于作者
* Github: [DebugST](https://github.com/DebugST/)
* Blog: [Crystal_lz](http://st233.com)
* Mail: (2212233137@qq.com)
