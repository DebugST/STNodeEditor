由于`3.0`版本可能部分代码会被重构，所以决定开一个新的项目。但是项目目前并未上传`GitHub`。将在完成时候上传。

虽然用户量不多，但是感谢那些一直在使用`STNodeEditor`的用户，在他们那里作者得到了一些反馈。将做如下调整：

|内容|状态|完成时间|备注|
|:---|:---|:---|:---|
|增加高DPI支持             |✅  |2022-09-12|创建了 `STGraphics`|
|添加json格式序列化文件    |✅  |2022-09-30|添加新项目 [STJson](https://github.com/DebugST/STJson)|
|添加`STNodeEditorCanvas`  |☑️ |开始|-|
|添加缩略图                |☑️ |-|-|
|节点选项悬浮提示信息      |☑️ |-|-|
|修复已知bug               |☑️ |-|-|

添加控件支持：

|内容|状态|完成时间|备注|
|:---|:---|:---|:---|
|Panel          |☑️     |-|-|
|lable          |☑️     |-|-|
|button         |☑️     |-|-|
|textbox        |☑️     |-|-|
|listview       |☑️     |-|-|
|chekcbox       |☑️     |-|-|
|radiobutton    |☑️     |-|-|
|combobox       |☑️     |-|-|
|groupbox       |☑️     |-|-|
|picturebox     |☑️     |-|-|
|progressbar    |☑️     |-|-|
|trackbar       |☑️     |-|-|
|NumericUpDown  |☑️     |-|-|

控件将尽可能保持`WinForm`的使用习惯。
    
添加`STNodeEditorCanvas.cs`

替代原本的`STNodeEditor.cs`

```cs
var canvas = new STNodeEditorCanvas("layer_name");
canvas.Nodes.add(new STNode_1());
canvas.Nodes.add(new STNode_2());
STNodeEditor_1.Canvas = canvas;
// STNodeEditor.Layers.Add(canvas) 此方式先待定
/*
    这样做的目的是想实现类似TabControl的效果，用户可能有多个画布需要加载，
    所以他不得不添加多个STNodeEditor做切换。所以用STNodeEditorCanvas代替原本的STNodeEditor
    而STNodeEditor仅仅作为一个画布容器
*/
```

添加`STNodeSpy.cs`

虽然`STNodeEditor`可以通过选项点颜色来区分数据类型，但是不排除会出现同一个颜色不同数据类型的情况，尤其是节点并非一个人开发的情况。

`STNodeSpy`作为一个内置节点提供就像`STNodeHub`一样，它的使用方式将会和`SPY++`类似。


添加`STNodeGroup.cs`

目前的`STNodeEditor`并没有分组的功能，作者准备尝试使用`Blender`一样的分组方式，毕竟作者确实很喜欢`Blender`。将分组作为一个节点使用。

```cs
var group = new STNodeGroup("group_name");
group.Nodes.Add(new STNode_1());
group.Nodes.Add(new STNode_2());
var layer = new STNodeEditorLayer("layer_name");
layder.Nodes.Add(group);
/*
    group 具有可编辑属性，它将以普通节点的形式存在于layer中。但是它可以被展开
    当group被展开时，它将变成一个独立的layer，可以进行节点的添加和删除。
    可以参考Blender的group，可以理解为将整个画布的最开始的输入和最终的输出作为一个节点的输入和输出。
*/
```

以上仅供参考，最终效果请以实物为准。😏😏😏😏😏(毕竟作者是个死咸鱼，不排除摆烂、虚假宣传的可能)。

如上面的某一个功能已经完成则会在旁边标记一个时间 比如：

(2023-12-21)`增加高DPI支持`

如果你有什么想法可以联系咸鱼作者：2212233137@qq.com