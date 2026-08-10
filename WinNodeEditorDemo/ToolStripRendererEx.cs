using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace WinNodeEditorDemo
{
    public class ToolStripRendererEx : ToolStripRenderer
    {
        private SolidBrush m_brush = new SolidBrush(Color.FromArgb(255, 52, 86, 141));
        private StringFormat m_sf = new StringFormat();

        public ToolStripRendererEx() {
            m_sf.LineAlignment = StringAlignment.Center;
        }

        protected override void InitializeItem(ToolStripItem item) {
            base.InitializeItem(item);
            item.AutoSize = false;
            item.Size = new Size(item.Width, 30);
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) {
            using (SolidBrush sb = new SolidBrush(Color.FromArgb(34, 34, 34))) {
                e.Graphics.FillRectangle(sb, e.AffectedBounds);
            }
            base.OnRenderToolStripBackground(e);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) {
            e.Graphics.DrawRectangle(Pens.Black, e.AffectedBounds.X, e.AffectedBounds.Y, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1);
            base.OnRenderToolStripBorder(e);
        }

        //protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) {
        //    using (SolidBrush sb = new SolidBrush(Color.FromArgb(50, 50, 50))) {
        //        e.Graphics.FillRectangle(sb, e.AffectedBounds);
        //    }
        //    base.OnRenderImageMargin(e);
        //}

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) {
            e.TextColor = e.Item.Selected ? Color.White : Color.LightGray;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            e.TextRectangle = new Rectangle(e.TextRectangle.Left, e.TextRectangle.Top, e.TextRectangle.Width, 30);
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e) {
            e.ArrowColor = e.Item.Selected ? Color.White : Color.LightGray;
            base.OnRenderArrow(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e) {
            Point ptEnd = new Point(e.Item.ContentRectangle.X + e.Item.Width / 2, e.Item.ContentRectangle.Y);
            using (LinearGradientBrush lgb = new LinearGradientBrush(e.Item.ContentRectangle.Location, ptEnd, Color.Transparent, Color.Gray)) {
                lgb.WrapMode = WrapMode.TileFlipX;
                using (Pen p = new Pen(lgb)) {
                    e.Graphics.DrawLine(p, e.Item.ContentRectangle.Location, new Point(e.Item.ContentRectangle.Right, ptEnd.Y));
                }
            }
            //e.Graphics.DrawLine(Pens.Gray, e.Item.ContentRectangle.Location, new Point(e.Item.ContentRectangle.Right, ptEnd.Y));
            base.OnRenderSeparator(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e) {
            if (e.Item.Selected)
                e.Graphics.FillRectangle(m_brush, e.Item.ContentRectangle);
            else
                base.OnRenderMenuItemBackground(e);
        }

        //protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e) {
        //    //base.OnRenderItemImage(e);
        //    e.Graphics.DrawImage(e.Image, e.ImageRectangle.X, e.ImageRectangle.Y, 17, 17);
        //}
    }
}
