using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace ST.Library.UI.Demo_Image
{
    public class STNodeImage : STNode
    {
        protected STNodeOption m_input_image;   //输入输出点
        protected STNodeOption m_out_image;

        protected override void OnCreate() {
            base.OnCreate();
            m_input_image = new STNodeOption("", typeof(Image), true);
            this.InputOptions.Add(m_input_image);
            m_out_image = new STNodeOption("", typeof(Image), false);
            this.OutputOptions.Add(m_out_image);
            m_input_image.DataTransfer += new STNodeOptionEventHandler(m_input_image_DataTransfer);
            this.Title = "Image";
        }
        //监听输入点接入事件
        void m_input_image_DataTransfer(object sender, STNodeOptionEventArgs e) {
            if (e.Status != ConnectionStatus.Connected)
                m_input_image.Data = null;
            else
                m_input_image.Data = e.TargetOption.Data;
            m_out_image.TransferData(m_input_image.Data);       //输出节点向下投递数据
            this.OnDataTransfer();                              //通知子类
            this.Invalidate();                                  //重绘自己
        }

        protected override System.Drawing.Size OnBuildNodeSize(DrawingTools dt) {
            //return base.OnBuildNodeSize();
            return new System.Drawing.Size(160, 120);           //设定节点大小
        }

        protected override void OnDrawBody(DrawingTools dt) {   //重绘节点主体部分
            base.OnDrawBody(dt);
            Graphics g = dt.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            if (m_input_image.Data != null) {
                g.DrawImage((Image)m_input_image.Data, this.Left + 15, this.Top + 30, this.Width - 40, this.Height - 40);
            } else {
                g.FillRectangle(Brushes.Gray, this.Left + 15, this.Top + 30, this.Width - 40, this.Height - 40);
            }
        }

        protected virtual void OnDataTransfer() { }
    }
}
