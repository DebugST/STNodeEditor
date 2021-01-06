## STNodeEditor

`STNodeEditor` 是一个很不错的节点编辑器 使用方式非常简洁 提供了丰富的属性以及事件 可以非常方便的完成节点之间数据的交互及通知 大量的重载函数供开发者使用具有很高的自由性

编译环境: `VS 2010 (.Net 3.5)`

![STNodeEditor](https://debugst.github.io/DotNet_WinForm_NodeEditor/images/node.png)

此控件采用 [`MIT`](https://opensource.org/licenses/mit-license.php) 开源协议开源 使开发者能够拥有更大的自由度更少的限制

`STNodeEditor` 拥有非常方便的UI自定义能力 提供的 `STNodeControl` 基类 可以让开发者能够像自定义 `WinForm` 控件一样去定义 节点需要的控件

![STNodeEditor](https://debugst.github.io/DotNet_WinForm_NodeEditor/images/formImage.png)

## 简要说明
* 画布
	* 移动 `鼠标中键` 拖动 Mac用户可二指拖动 `触控板`
	* 缩放 按下 `Control` + `鼠标滚轮`
	* 画布中的节点内容以及连线关系可通过 `STNodeEditor.Load/SaveCanvas()` 加载或者保存
* 删除连线
	* 悬停到连线上 `鼠标右键`
* 移动节点
	* `鼠标左键` 拖动 `节点标题`
	* 之所以是拖动标题而不是节点任意位置 是因为作者的设计思路是将节点视为一个 `窗体` 窗体的客户区域留给开发者自定义
* STNode
	* 如同 `System.Windows.Forms.TreeView` 一样 所有的节点都保存在 `STNodeEditor.Nodes` 中 其数据类型为 `STNode`
	* `STNode` 为抽象类被 `abstract` 修饰 需要开发者自己继承向节点中添加选项
	* `STNode` 有三个重要属性 `InputOptions` `OutputOptions` `Controls`

## 示例
```cs
public class DemoNode : STNode
{
    protected override void OnCreate() {
	    //在创建节点时候向其添加需要的选项
        base.OnCreate();
        this.InputOptions.Add(new STNodeOption("Input", typeof(string), false));
        this.InputOptions.Add(new STNodeOption("SingleNode", typeof(System.Drawing.Image), true));
        this.InputOptions.Add(new STNodeOption("SingleNode", typeof(object), true));

        this.OutputOptions.Add(new STNodeOption("output", typeof(string), false));
        this.OutputOptions.Add(new STNodeOption("Single", typeof(System.Drawing.Icon), true));
        this.OutputOptions.Add(new STNodeOption("Single", typeof(object), true));

        this.Title = "Demo_Node";
    }
}
//stNodeEditor1.SetTypeColor(type, color);此方法会自动替换已存在值
stNodeEditor1.TypeColor.Add(typeof(string), Color.Yellow);
stNodeEditor1.TypeColor.Add(typeof(Image), Color.Red);
stNodeEditor1.Nodes.Add(new DemoNode());
```
上述代码的 `DemoNode` 被添加到节点编辑器中的效果为

![STNodeEditor](https://debugst.github.io/DotNet_WinForm_NodeEditor/images/node_demo.png)

由此可见 开发者要自定义一个节点相当便捷 当然上述代码并没有包含事件处理 上述代码仅仅是在演示 `STNodeOption` 的效果 下列代码则是一个包含了计算两个整数和的功能
```cs
public class NodeNumberAdd : STNode
{
    private STNodeOption m_in_num1;
    private STNodeOption m_in_num2;
    private STNodeOption m_out_num;
    private int m_nNum1, m_nNum2;

    protected override void OnCreate() {
        base.OnCreate();
        this.Title = "NumberAdd";
        m_in_num1 = new STNodeOption("num1", typeof(int), true);//只能有一个连线
        m_in_num2 = new STNodeOption("num2", typeof(int), true);//只能有一个连线
        m_out_num = new STNodeOption("result", typeof(int), false);//可以多个连线
        this.InputOptions.Add(m_in_num1);
        this.InputOptions.Add(m_in_num2);
        this.OutputOptions.Add(m_out_num);
        m_in_num1.DataTransfer += new STNodeOptionEventHandler(m_in_num_DataTransfer);
        m_in_num2.DataTransfer += new STNodeOptionEventHandler(m_in_num_DataTransfer);
    }
    //当有数据传入时
    void m_in_num_DataTransfer(object sender, STNodeOptionEventArgs e) {
        //判断连线是否是连接状态(建立连线 断开连线 都会触发该事件)
        if (e.Status == ConnectionStatus.Connected) {
            if (sender == m_in_num1)
                m_nNum1 = (int)e.TargetOption.Data;//TargetOption为触发此事件的Option
            else
                m_nNum2 = (int)e.TargetOption.Data;
        } else {
            if (sender == m_in_num1) m_nNum1 = 0; else m_nNum2 = 0;
        }
        //向输出选项上的所有连线传输数据 输出选项上的所有连线都会触发 DataTransfer 事件
        m_out_num.TransferData(m_nNum1 + m_nNum2); //m_out_num.Data 将被自动设置
    }

    protected override void OnOwnerChanged() {
	    //通常刚被添加到节点编辑器时触发 如是以插件方式提供的节点 应当向编辑器提交数据类型颜色
        base.OnOwnerChanged();
        if (this.Owner == null) return; //或者通过m_in_num1.DotColor = Color.Red;进行设置
        this.Owner.SetTypeColor(typeof(int),Color.Red);
    }
}
```
效果为

![STNodeEditor](https://debugst.github.io/DotNet_WinForm_NodeEditor/images/node_add.png)

更多细节请参考文档

项目主页:  [DebugST.github.io/DotNet_WinForm_NodeEditor](https://DebugST.github.io/DotNet_WinForm_NodeEditor)
开发文档: [DebugST.github.io/DotNet_WinForm_NodeEditor/doc.html](https://DebugST.github.io/DotNet_WinForm_NodeEditor/doc.html)
##关于作者
* Github: [DebugST](https://DebugST.github.io)
* Blog: [Crystal_lz](http://st233.com)
* Mail: (2212233137@qq.com)
