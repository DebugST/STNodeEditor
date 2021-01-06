using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using System.Windows.Forms;

namespace ST.Library.UI.Demo_Image
{
    public class STNodeImageInput : STNode
    {
        private STNodeOption m_option_out;

        private string m_str_file;
        private Size m_sz = new Size(100, 60);

        protected override void OnCreate() {
            base.OnCreate();
            this.Title = "ImageInput";
            m_option_out = new STNodeOption("", typeof(Image), false);
            this.OutputOptions.Add(m_option_out);
            STNodeButton btn = new STNodeButton();
            btn.Left = (m_sz.Width - btn.Width) / 2;
            btn.Top = (m_sz.Height - 20 - btn.Height) / 2;
            btn.Text = "OpenImage";
            btn.MouseClick += new MouseEventHandler(btn_MouseClick);
            this.Controls.Add(btn);
        }

        void btn_MouseClick(object sender, MouseEventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "*.jpg|*.jpg|*.png|*.png|*.bmp|*.bmp|*.*|*.*";
            if (ofd.ShowDialog() != DialogResult.OK) return;
            m_option_out.TransferData(Image.FromFile(ofd.FileName));
            m_str_file = ofd.FileName;
        }

        protected override Size OnBuildNodeSize(DrawingTools dt) {
            //return base.OnBuildNodeSize();
            return m_sz;
        }

        protected override Point OnSetOptionLocation(STNodeOption op) {
            return new Point(op.DotLeft, this.Top + 35);
            //return base.OnSetOptionLocation(op);
        }

        //protected override void OnDrawOptionDot(DrawingTools dt, STNodeOption op) {
        //    //if (op == m_option_out) op.DotTop = this.Top + 35;
        //    base.OnDrawOptionDot(dt, op);
        //}

        protected override void OnSaveNode(Dictionary<string, byte[]> dic) {
            if (m_str_file == null) m_str_file = string.Empty;
            dic.Add("file", Encoding.UTF8.GetBytes(m_str_file));
        }

        protected override void OnLoadNode(Dictionary<string, byte[]> dic) {
            base.OnLoadNode(dic);
            m_str_file = Encoding.UTF8.GetString(dic["file"]);
            if (System.IO.File.Exists(m_str_file)) {   //如果文件存在加载并投递数据
                m_option_out.TransferData(Image.FromFile(m_str_file));
            }
        }

        public class STNodeButton : STNodeControl   //自定义一个Button控件
        {
            private bool m_isHover;

            protected override void OnMouseEnter(EventArgs e) {
                base.OnMouseEnter(e);
                m_isHover = true;
                this.Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e) {
                base.OnMouseLeave(e);
                m_isHover = false;
                this.Invalidate();
            }

            protected override void OnPaint(DrawingTools dt) {
                //base.OnPaint(dt);
                Graphics g = dt.Graphics;
                SolidBrush brush = dt.SolidBrush;
                brush.Color = m_isHover ? Color.DodgerBlue : this.BackColor;
                g.FillRectangle(brush, 0, 0, this.Width, this.Height);
                g.DrawString(this.Text, this.Font, Brushes.White, this.ClientRectangle, base.m_sf);
            }
        }
    }
}
