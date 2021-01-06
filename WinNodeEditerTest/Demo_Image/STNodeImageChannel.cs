using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace ST.Library.UI.Demo_Image
{
    public class STNodeImageChannel : STNodeImage
    {
        private STNodeOption m_out_r;
        private STNodeOption m_out_g;
        private STNodeOption m_out_b;

        private Bitmap m_img_r;
        private Bitmap m_img_g;
        private Bitmap m_img_b;

        protected override void OnCreate() {
            base.OnCreate();
            m_out_r = new STNodeOption("R", typeof(Image), false);
            m_out_g = new STNodeOption("G", typeof(Image), false);
            m_out_b = new STNodeOption("B", typeof(Image), false);
            this.OutputOptions.Add(m_out_r);
            this.OutputOptions.Add(m_out_g);
            this.OutputOptions.Add(m_out_b);
            this.Title = "Channel";
        }

        protected override void OnDataTransfer() {
            base.OnDataTransfer();
            if (m_img_r != null) {
                m_img_r.Dispose();
                m_img_g.Dispose();
                m_img_b.Dispose();
                m_img_r = m_img_g = m_img_b = null;
            }
            if (m_out_image.Data != null) {     //分离通道 Demo 演示 Get/SetPixel() 效率极低 应当LockBitmap操作
                Bitmap img = (Bitmap)base.m_input_image.Data;
                m_img_r = new Bitmap(img.Width, img.Height);
                m_img_g = new Bitmap(img.Width, img.Height);
                m_img_b = new Bitmap(img.Width, img.Height);
                for (int x = 0; x < img.Width; x++) {
                    for (int y = 0; y < img.Height; y++) {
                        Color clr = img.GetPixel(x, y);
                        m_img_r.SetPixel(x, y, Color.FromArgb(255, clr.R, clr.R, clr.R));
                        m_img_g.SetPixel(x, y, Color.FromArgb(255, clr.G, clr.G, clr.G));
                        m_img_b.SetPixel(x, y, Color.FromArgb(255, clr.B, clr.B, clr.B));
                    }
                }
            }
            m_out_r.TransferData(m_img_r);
            m_out_g.TransferData(m_img_b);
            m_out_b.TransferData(m_img_b);
        }
    }
}
