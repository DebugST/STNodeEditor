using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ST.Library.UI.NodeEditor
{
    internal class FrmNodePreviewPanel : Form
    {
        public Color BorderColor { get; set; }
        public bool AutoBorderColor { get; set; }

        private bool m_bRight;
        private Point m_ptHandle;
        private int m_nHandleSize;
        private Rectangle m_rect_handle;
        private Rectangle m_rect_panel;
        private Rectangle m_rect_exclude;
        private Region m_region;
        private Type m_type;
        private STNode m_node;
        private STNodeEditor m_editor;
        private STNodePropertyGrid m_property;

        private Pen m_pen = new Pen(Color.Black);
        private SolidBrush m_brush = new SolidBrush(Color.Black);
        private static FrmNodePreviewPanel m_last_frm;

        [DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        public FrmNodePreviewPanel(Type stNodeType, Point ptHandle, int nHandleSize, bool bRight, STNodeEditor editor, STNodePropertyGrid propertyGrid) {
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            if (m_last_frm != null) m_last_frm.Close();
            m_last_frm = this;

            m_editor = editor;
            m_property = propertyGrid;
            m_editor.Size = new Size(200, 200);
            m_property.Size = new Size(200, 200);
            m_editor.Location = new Point(1 + (bRight ? nHandleSize : 0), 1);
            m_property.Location = new Point(m_editor.Right, 1);
            m_property.InfoFirstOnDraw = true;
            this.Controls.Add(m_editor);
            this.Controls.Add(m_property);
            this.ShowInTaskbar = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Size = new Size(402 + nHandleSize, 202);

            m_type = stNodeType;
            m_ptHandle = ptHandle;
            m_nHandleSize = nHandleSize;
            m_bRight = bRight;

            this.AutoBorderColor = true;
            this.BorderColor = Color.DodgerBlue;
        }

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            m_node = (STNode)Activator.CreateInstance(m_type);
            m_node.Left = 20; m_node.Top = 20;
            m_editor.Nodes.Add(m_node);
            m_property.SetNode(m_node);

            m_rect_panel = new Rectangle(0, 0, 402, 202);
            m_rect_handle = new Rectangle(m_ptHandle.X, m_ptHandle.Y, m_nHandleSize, m_nHandleSize);
            m_rect_exclude = new Rectangle(0, m_nHandleSize, m_nHandleSize, this.Height - m_nHandleSize);
            if (m_bRight) {
                this.Left = m_ptHandle.X;
                m_rect_panel.X = m_ptHandle.X + m_nHandleSize;
            } else {
                this.Left = m_ptHandle.X - this.Width + m_nHandleSize;
                m_rect_exclude.X = this.Width - m_nHandleSize;
                m_rect_panel.X = this.Left;
            }
            if (m_ptHandle.Y + this.Height > Screen.GetWorkingArea(this).Bottom) {
                this.Top = m_ptHandle.Y - this.Height + m_nHandleSize;
                m_rect_exclude.Y -= m_nHandleSize;
            } else this.Top = m_ptHandle.Y;
            m_rect_panel.Y = this.Top;
            m_region = new Region(new Rectangle(Point.Empty, this.Size));
            m_region.Exclude(m_rect_exclude);
            using (Graphics g = this.CreateGraphics()) {
                IntPtr h = m_region.GetHrgn(g);
                FrmNodePreviewPanel.SetWindowRgn(this.Handle, h, false);
                m_region.ReleaseHrgn(h);
            }

            this.MouseLeave += Event_MouseLeave;
            m_editor.MouseLeave += Event_MouseLeave;
            m_property.MouseLeave += Event_MouseLeave;
            this.BeginInvoke(new MethodInvoker(() => {
                m_property.Focus();
            }));
        }

        protected override void OnClosing(CancelEventArgs e) {
            base.OnClosing(e);
            this.Controls.Clear();
            m_editor.Nodes.Clear();
            m_editor.MouseLeave -= Event_MouseLeave;
            m_property.MouseLeave -= Event_MouseLeave;
            m_last_frm = null;
        }

        void Event_MouseLeave(object sender, EventArgs e) {
            Point pt = Control.MousePosition;
            if (m_rect_panel.Contains(pt) || m_rect_handle.Contains(pt)) return;
            this.Close();
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            m_pen.Color = this.AutoBorderColor ? m_node.TitleColor : this.BorderColor;
            m_brush.Color = m_pen.Color;
            g.DrawRectangle(m_pen, 0, 0, this.Width - 1, this.Height - 1);
            g.FillRectangle(m_brush, m_rect_exclude.X - 1, m_rect_exclude.Y - 1, m_rect_exclude.Width + 2, m_rect_exclude.Height + 2);

            Rectangle rect = this.RectangleToClient(m_rect_handle);
            rect.Y = (m_nHandleSize - 14) / 2;
            rect.X += rect.Y + 1;
            rect.Width = rect.Height = 14;
            m_pen.Width = 2;
            g.DrawLine(m_pen, rect.X + 4, rect.Y + 3, rect.X + 10, rect.Y + 3);
            g.DrawLine(m_pen, rect.X + 4, rect.Y + 6, rect.X + 10, rect.Y + 6);
            g.DrawLine(m_pen, rect.X + 4, rect.Y + 11, rect.X + 10, rect.Y + 11);
            g.DrawLine(m_pen, rect.X + 7, rect.Y + 7, rect.X + 7, rect.Y + 10);
            m_pen.Width = 1;
            g.DrawRectangle(m_pen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
        }
    }
}
