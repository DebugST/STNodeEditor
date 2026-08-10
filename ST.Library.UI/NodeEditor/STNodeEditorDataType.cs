using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Drawing;

namespace ST.Library.UI.NodeEditor
{
    public enum ConnectionStatus
    {
        /// <summary>
        /// 不存在所有者
        /// </summary>
        [Description("不存在所有者")]
        NoOwner,
        /// <summary>
        /// 相同的所有者
        /// </summary>
        [Description("相同的所有者")]
        SameOwner,
        /// <summary>
        /// 均为输入或者输出选项
        /// </summary>
        [Description("均为输入或者输出选项")]
        SameInputOrOutput,
        /// <summary>
        /// 不同的数据类型
        /// </summary>
        [Description("不同的数据类型")]
        ErrorType,
        /// <summary>
        /// 单连接节点
        /// </summary>
        [Description("单连接节点")]
        SingleOption,
        /// <summary>
        /// 出现环形路径
        /// </summary>
        [Description("出现环形路径")]
        Loop,
        /// <summary>
        /// 已存在的连接
        /// </summary>
        [Description("已存在的连接")]
        Exists,
        /// <summary>
        /// 空白选项
        /// </summary>
        [Description("空白选项")]
        EmptyOption,
        /// <summary>
        /// 已经连接
        /// </summary>
        [Description("已经连接")]
        Connected,
        /// <summary>
        /// 连接被断开
        /// </summary>
        [Description("连接被断开")]
        DisConnected,
        /// <summary>
        /// 节点被锁定
        /// </summary>
        [Description("节点被锁定")]
        Locked,
        /// <summary>
        /// 操作被拒绝
        /// </summary>
        [Description("操作被拒绝")]
        Reject,
        /// <summary>
        /// 正在被连接
        /// </summary>
        [Description("正在被连接")]
        Connecting,
        /// <summary>
        /// 正在断开连接
        /// </summary>
        [Description("正在断开连接")]
        DisConnecting
    }

    public enum AlertLocation
    {
        Left,
        Top,
        Right,
        Bottom,
        Center,
        LeftTop,
        RightTop,
        RightBottom,
        LeftBottom,
    }

    public struct DrawingTools
    {
        public Graphics Graphics;
        public Pen Pen;
        public SolidBrush SolidBrush;
    }

    public enum CanvasMoveArgs      //移动画布时需要的参数 查看->MoveCanvas()
    {
        Left = 1,                   //表示 仅移动 X 坐标
        Top = 2,                    //表示 仅移动 Y 坐标
        All = 4                     //表示 X Y 同时移动
    }

    public struct NodeFindInfo
    {
        public STNode Node;
        public STNodeOption NodeOption;
        public string Mark;
        public string[] MarkLines;
    }

    public struct ConnectionInfo
    {
        public STNodeOption Input;
        public STNodeOption Output;
    }

    public delegate void STNodeOptionEventHandler(object sender, STNodeOptionEventArgs e);

    public class STNodeOptionEventArgs : EventArgs
    {
        private STNodeOption _TargetOption;
        /// <summary>
        /// 触发此事件的对应Option
        /// </summary>
        public STNodeOption TargetOption {
            get { return _TargetOption; }
        }

        private ConnectionStatus _Status;
        /// <summary>
        /// Option之间的连线状态
        /// </summary>
        public ConnectionStatus Status {
            get { return _Status; }
            internal set { _Status = value; }
        }

        private bool _IsSponsor;
        /// <summary>
        /// 是否为此次行为的发起者
        /// </summary>
        public bool IsSponsor {
            get { return _IsSponsor; }
        }

        public STNodeOptionEventArgs(bool isSponsor, STNodeOption opTarget, ConnectionStatus cr) {
            this._IsSponsor = isSponsor;
            this._TargetOption = opTarget;
            this._Status = cr;
        }
    }

    public delegate void STNodeEditorEventHandler(object sender, STNodeEditorEventArgs e);
    public delegate void STNodeEditorOptionEventHandler(object sender, STNodeEditorOptionEventArgs e);


    public class STNodeEditorEventArgs : EventArgs
    {
        private STNode _Node;

        public STNode Node {
            get { return _Node; }
        }

        public STNodeEditorEventArgs(STNode node) {
            this._Node = node;
        }
    }

    public class STNodeEditorOptionEventArgs : STNodeOptionEventArgs
    {

        private STNodeOption _CurrentOption;
        /// <summary>
        /// 主动触发事件的Option
        /// </summary>
        public STNodeOption CurrentOption {
            get { return _CurrentOption; }
        }

        private bool _Continue = true;
        /// <summary>
        /// 是否继续向下操作 用于Begin(Connecting/DisConnecting)是否继续向后操作
        /// </summary>
        public bool Continue {
            get { return _Continue; }
            set { _Continue = value; }
        }

        public STNodeEditorOptionEventArgs(STNodeOption opTarget, STNodeOption opCurrent, ConnectionStatus cr)
            : base(false, opTarget, cr) {
            this._CurrentOption = opCurrent;
        }
    }
}
