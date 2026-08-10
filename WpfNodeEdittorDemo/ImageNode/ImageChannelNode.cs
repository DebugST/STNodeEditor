using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ST.Library.UI.NodeEditor;
using System.Drawing;
using System.Drawing.Imaging;

namespace WinNodeEditorDemo.ImageNode
{
    [STNode("/Image")]
    public class ImageChannelNode : ImageBaseNode
    {
        private STNodeOption m_op_img_in;   //输入的节点
        private STNodeOption m_op_img_r;    //R图 输出节点
        private STNodeOption m_op_img_g;    //G图 输出节点
        private STNodeOption m_op_img_b;    //B图 输出节点

        protected override void OnCreate() {
            base.OnCreate();
            this.Title = "ImageChannel";

            m_op_img_in = this.InputOptions.Add("", typeof(Image), true);
            m_op_img_r = this.OutputOptions.Add("R", typeof(Image), false);
            m_op_img_g = this.OutputOptions.Add("G", typeof(Image), false);
            m_op_img_b = this.OutputOptions.Add("B", typeof(Image), false);
            //当输入节点有数据输入时候
            m_op_img_in.DataTransfer += new STNodeOptionEventHandler(m_op_img_in_DataTransfer);
        }

        void m_op_img_in_DataTransfer(object sender, STNodeOptionEventArgs e) {
            //如果当前不是连接状态 或者 接受到的数据为空
            if (e.Status != ConnectionStatus.Connected || e.TargetOption.Data == null) {
                m_op_img_out.TransferData(null);    //向所有输出节点输出空数据
                m_op_img_r.TransferData(null);
                m_op_img_g.TransferData(null);
                m_op_img_b.TransferData(null);
                m_img_draw = null;                  //需要绘制显示的图片置为空
            } else {
                Bitmap bmp = (Bitmap)e.TargetOption.Data;           //否则计算图片的RGB图像
                Bitmap bmp_r = new Bitmap(bmp.Width, bmp.Height);
                Bitmap bmp_g = new Bitmap(bmp.Width, bmp.Height);
                Bitmap bmp_b = new Bitmap(bmp.Width, bmp.Height);
                BitmapData bmpData = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                BitmapData bmpData_r = bmp_r.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                BitmapData bmpData_g = bmp_g.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                BitmapData bmpData_b = bmp_b.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                byte[] byColor = new byte[bmpData.Height * bmpData.Stride];
                byte[] byColor_r = new byte[byColor.Length];
                byte[] byColor_g = new byte[byColor.Length];
                byte[] byColor_b = new byte[byColor.Length];
                System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, byColor, 0, byColor.Length);
                for (int y = 0; y < bmpData.Height; y++) {
                    int ny = y * bmpData.Stride;
                    for (int x = 0; x < bmpData.Width; x++) {
                        int nx = x << 2;
                        byColor_b[ny + nx] = byColor[ny + nx];
                        byColor_g[ny + nx + 1] = byColor[ny + nx + 1];
                        byColor_r[ny + nx + 2] = byColor[ny + nx + 2];
                        byColor_r[ny + nx + 3] = byColor_g[ny + nx + 3] = byColor_b[ny + nx + 3] = byColor[ny + nx + 3];
                    }
                }
                bmp.UnlockBits(bmpData);
                System.Runtime.InteropServices.Marshal.Copy(byColor_r, 0, bmpData_r.Scan0, byColor_r.Length);
                System.Runtime.InteropServices.Marshal.Copy(byColor_g, 0, bmpData_g.Scan0, byColor_g.Length);
                System.Runtime.InteropServices.Marshal.Copy(byColor_b, 0, bmpData_b.Scan0, byColor_b.Length);
                bmp_r.UnlockBits(bmpData_r);
                bmp_g.UnlockBits(bmpData_g);
                bmp_b.UnlockBits(bmpData_b);
                m_op_img_out.TransferData(bmp); //out选项 输出原图
                m_op_img_r.TransferData(bmp_r); //R选项输出R图
                m_op_img_g.TransferData(bmp_g);
                m_op_img_b.TransferData(bmp_b);
                m_img_draw = bmp;               //需要绘制显示的图片
            }
        }

        protected override void OnDrawBody(DrawingTools dt) {
            base.OnDrawBody(dt);
            Graphics g = dt.Graphics;
            Rectangle rect = new Rectangle(this.Left + 10, this.Top + 30, 120, 80);
            g.FillRectangle(Brushes.Gray, rect);
            if (m_img_draw != null) g.DrawImage(m_img_draw, rect);
        }
    }
}
