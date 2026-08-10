using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;
using ST.Library.UI.NodeEditor;

namespace WinNodeEditorDemo.Blender
{
    /// <summary>
    /// 此类仅演示 作为MixRGB节点的下拉框控件
    /// </summary>
    public class STNodeSelectEnumBox : STNodeControl
    {
        private Enum _Enum;
        public Enum Enum {
            get { return _Enum; }
            set {
                _Enum = value;
                this.Invalidate();
            }
        }

        public event EventHandler ValueChanged;
        protected virtual void OnValueChanged(EventArgs e) {
            if (this.ValueChanged != null) this.ValueChanged(this, e);
        }

        protected override void OnPaint(DrawingTools dt) {
            Graphics g = dt.Graphics;
            dt.SolidBrush.Color = Color.FromArgb(80, 0, 0, 0);
            g.FillRectangle(dt.SolidBrush, this.ClientRectangle);
            m_sf.Alignment = StringAlignment.Near;
            g.DrawString(this.Enum.ToString(), this.Font, Brushes.White, this.ClientRectangle, m_sf);
            g.FillPolygon(Brushes.Gray, new Point[]{
                new Point(this.Right - 25, 7),
                new Point(this.Right - 15, 7),
                new Point(this.Right - 20, 12)
            });
        }

        protected override void OnMouseClick(System.Windows.Forms.MouseEventArgs e) {
            base.OnMouseClick(e);
            Point pt = new Point(this.Left + this.Owner.Left, this.Top + this.Owner.Top + this.Owner.TitleHeight);
            pt = this.Owner.Owner.CanvasToControl(pt);
            pt = this.Owner.Owner.PointToScreen(pt);
            FrmEnumSelect frm = new FrmEnumSelect(this.Enum, pt, this.Width, this.Owner.Owner.CanvasScale);
            var v = frm.ShowDialog();
            if (v != System.Windows.Forms.DialogResult.OK) return;
            this.Enum = frm.Enum;
            this.OnValueChanged(new EventArgs());
        }
    }
}
