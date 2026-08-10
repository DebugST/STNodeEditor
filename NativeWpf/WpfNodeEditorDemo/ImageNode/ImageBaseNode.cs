using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ST.Library.UI.NodeEditor;
using System.Drawing;

namespace WinNodeEditorDemo.ImageNode
{
    /// <summary>
    /// 图片节点基类 用于确定节点风格 标题颜色 以及 数据类型颜色
    /// </summary>
    public abstract class ImageBaseNode : STNode
    {
        /// <summary>
        /// 需要作为显示绘制的图片
        /// </summary>
        protected Image m_img_draw;
        /// <summary>
        /// 输出节点
        /// </summary>
        protected STNodeOption m_op_img_out;

        protected override void OnCreate() {
            base.OnCreate();
            m_op_img_out = this.OutputOptions.Add("", typeof(Image), false);
            this.AutoSize = false;          //此节点需要定制UI 所以无需AutoSize
            //this.Size = new Size(320,240);
            this.Width = 160;               //手动设置节点大小
            this.Height = 120;
            this.TitleColor = Color.FromArgb(200, Color.DarkCyan);
        }

        protected override void OnOwnerChanged() {  //向编辑器提交数据类型颜色
            base.OnOwnerChanged();
            if (this.Owner == null) return;
            this.Owner.SetTypeColor(typeof(Image), Color.DarkCyan);
        }
    }
}
