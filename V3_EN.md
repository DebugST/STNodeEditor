Since the `3.0` version may be partially refactored, it was decided to start a new project. But the project is not currently uploaded to `GitHub`. Will upload when finished.

Although there are not many users, thanks to those who have been using `STNodeEditor`, where the author got some feedback. The following adjustments will be made:

`Fix known bugs`

`Add high DPI support`

`Add json format serialization file`

`Add mini-map`

`Node option hover hint text`
    

Add Controls：

    `Panel` 
    
    `lable` 
    
    `button` 
    
    `textbox` 
    
    `listview` 
    
    `chekcbox` 
    
    `radiobutton`
    
    `combobox` 
    
    `groupbox`
    
    `picturebox`
    
    `progressbar` 
    
    `trackbar` 
    
    `NumericUpDown`
    
Controls will maintain the `WinForm` usage habits as much as possible.
    
Add `STNodeEditorLayer.cs`

To replace `STNodeEditor.cs`

```cs
var layer = new STNodeEditorLayer("layer_name");
layer.Nodes.add(new STNode_1());
layer.Nodes.add(new STNode_2());
STNodeEditor_1.Layer = layer;
// STNodeEditor.Layers.Add(layer) This method is pending.
/*
    The purpose of this is to achieve a TabControl-like effect, where the user may have multiple canvases to load.
    So he had to add multiple STNodeEditors for switching. So use STNodeEditorLayer to replace the original STNodeEditor.
    And STNodeEditor only acts as a canvas container.
*/
```

Add `STNodeSpy.cs`

Although `STNodeEditor` can distinguish data types by option point color, it does not rule out that there will be different data types with the same color, especially if the node is not developed by one person.

`STNodeSpy` is provided as a built-in node just like `STNodeHub`, it will be used in a similar way to `SPY++`.


Add `STNodeGroup.cs`

The current `STNodeEditor` does not have the function of grouping. The author is going to try to use the same grouping method as `Blender`. After all, the author really likes `Blender`. Use the group as a node.

```cs
var group = new STNodeGroup("group_name");
group.Nodes.Add(new STNode_1());
group.Nodes.Add(new STNode_2());
var layer = new STNodeEditorLayer("layer_name");
layder.Nodes.Add(group);
/*
    The group has editable properties and it will exist in the layer as a normal node. But it can be expanded.
    When the group is expanded, it will become an independent layer, and nodes can be added and removed.
    You can refer to Blender's group, which can be understood as taking the initial input and final output of the entire canvas as the input and output of a node.
*/
```

The above is for reference only, please refer to the actual product for the final effect. 😏😏😏😏😏 (after all, the author is a dead salted fish, and the possibility of strikes and false propaganda cannot be ruled out).

If one of the above functions has been completed, a time will be marked next to it. For example:

(2023-12-21)`Add high DPI support`

If you have any ideas you can contact the author: 2212233137@qq.com