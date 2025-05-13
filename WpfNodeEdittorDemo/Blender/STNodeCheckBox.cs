using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;
using ST.Library.UI.NodeEditor;

namespace WinNodeEditorDemo.Blender
{
    /// <summary>
    /// 此类仅演示 作为MixRGB节点的复选框控件
    /// </summary>
    public class STNodeCheckBox : STNodeControl
    {
        private bool _Checked;

        public bool Checked {
            get { return _Checked; }
            set {
                _Checked = value;
                this.Invalidate();
            }
        }

        public event EventHandler ValueChanged;
        protected virtual void OnValueChanged(EventArgs e) {
            if (this.ValueChanged != null) this.ValueChanged(this, e);
        }

        protected override void OnMouseClick(System.Windows.Forms.MouseEventArgs e) {
            base.OnMouseClick(e);
            this.Checked = !this.Checked;
            this.OnValueChanged(new EventArgs());
        }

        protected override void OnPaint(DrawingTools dt) {
            //base.OnPaint(dt);
            Graphics g = dt.Graphics;
            g.FillRectangle(Brushes.Gray, 0, 5, 10, 10);
            m_sf.Alignment = StringAlignment.Near;
            g.DrawString(this.Text, this.Font, Brushes.LightGray, new Rectangle(15, 0, this.Width - 20, 20), m_sf);
            if (this.Checked) {
                g.FillRectangle(Brushes.Black, 2, 7, 6, 6);
            }
        }
    }
}
