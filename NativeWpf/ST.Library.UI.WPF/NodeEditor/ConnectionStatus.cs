using System.ComponentModel;

namespace ST.Library.UI.NodeEditor;

public enum ConnectionStatus
{
	[Description("不存在所有者")]
	NoOwner,
	[Description("相同的所有者")]
	SameOwner,
	[Description("均为输入或者输出选项")]
	SameInputOrOutput,
	[Description("不同的数据类型")]
	ErrorType,
	[Description("单连接节点")]
	SingleOption,
	[Description("出现环形路径")]
	Loop,
	[Description("已存在的连接")]
	Exists,
	[Description("空白选项")]
	EmptyOption,
	[Description("已经连接")]
	Connected,
	[Description("连接被断开")]
	DisConnected,
	[Description("节点被锁定")]
	Locked,
	[Description("操作被拒绝")]
	Reject,
	[Description("正在被连接")]
	Connecting,
	[Description("正在断开连接")]
	DisConnecting
}
