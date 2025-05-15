using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ST.Library.UI.NodeEditor;
using System.Drawing;

namespace WinNodeEditorDemo.Blender
{
    /// <summary>
    /// 此类仅演示 作为MixRGB节点的进度条控件
    /// </summary>
    public class STNodeProgress : STNodeControl
    {
        private int _Value = 50;

        public int Value {
            get { return _Value; }
            set { 
                _Value = value;
                this.Invalidate();
            }
        }

        private bool m_bMouseDown;

        public event EventHandler ValueChanged;
        protected virtual void OnValueChanged(EventArgs e) {
            if (this.ValueChanged != null) this.ValueChanged(this, e);
        }

        protected override void OnPaint(DrawingTools dt) {
            base.OnPaint(dt);
            Graphics g = dt.Graphics;
            g.FillRectangle(Brushes.Gray, this.ClientRectangle);
            g.FillRectangle(Brushes.CornflowerBlue, 0, 0, (int)((float)this._Value / 100 * this.Width), this.Height);
            m_sf.Alignment = StringAlignment.Near;
            g.DrawString(this.Text, this.Font, Brushes.White, this.ClientRectangle, m_sf);
            m_sf.Alignment = StringAlignment.Far;
            g.DrawString(((float)this._Value / 100).ToString("F2"), this.Font, Brushes.White, this.ClientRectangle, m_sf);

        }

        protected override void OnMouseDown(System.Windows.Forms.MouseEventArgs e) {
            base.OnMouseDown(e);
            m_bMouseDown = true;
        }

        protected override void OnMouseUp(System.Windows.Forms.MouseEventArgs e) {
            base.OnMouseUp(e);
            m_bMouseDown = false;
        }

        protected override void OnMouseMove(System.Windows.Forms.MouseEventArgs e) {
            base.OnMouseMove(e);
            if (!m_bMouseDown) return;
            int v = (int)((float)e.X / this.Width * 100);
            if (v < 0) v = 0;
            if (v > 100) v = 100;
            this._Value = v;
            this.OnValueChanged(new EventArgs());
            this.Invalidate();
        }
    }
}
