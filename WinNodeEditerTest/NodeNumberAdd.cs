using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace ST.Library.UI
{
    public class NodeNumberAdd : STNode
    {
        private STNodeOption m_in_num1;
        private STNodeOption m_in_num2;
        private STNodeOption m_out_num;
        private int m_nNum1, m_nNum2;

        protected override void OnCreate() {
            base.OnCreate();
            this.Title = "NumberAdd";
            m_in_num1 = new STNodeOption("num1", typeof(int), true);//只能有一个连线
            m_in_num2 = new STNodeOption("num2", typeof(int), true);//只能有一个连线
            m_out_num = new STNodeOption("result", typeof(int), false);//可以多个连线
            this.InputOptions.Add(m_in_num1);
            this.InputOptions.Add(m_in_num2);
            this.OutputOptions.Add(m_out_num);
            m_in_num1.DataTransfer += new STNodeOptionEventHandler(m_in_num_DataTransfer);
            m_in_num2.DataTransfer += new STNodeOptionEventHandler(m_in_num_DataTransfer);
        }
        //当有数据传入时
        void m_in_num_DataTransfer(object sender, STNodeOptionEventArgs e) {
            //判断连线是否是连接状态(建立连线 断开连线 都会触发该事件)
            if (e.Status == ConnectionStatus.Connected && e.TargetOption.Data != null) {
                if (sender == m_in_num1)
                    m_nNum1 = (int)e.TargetOption.Data;//TargetOption为触发此事件的Option
                else
                    m_nNum2 = (int)e.TargetOption.Data;
            } else {
                if (sender == m_in_num1) m_nNum1 = 0; else m_nNum2 = 0;
            }
            //向输出选项上的所有连线传输数据 输出选项上的所有连线都会触发 DataTransfer 事件
            m_out_num.TransferData(m_nNum1 + m_nNum2); //m_out_num.Data 将被自动设置
        }

        protected override void OnOwnerChanged() {
            base.OnOwnerChanged();//通常刚被添加到节点编辑器时触发 如是以插件方式提供的节点 应当向编辑器提交数据类型颜色
            if (this.Owner == null) return; //或者通过m_in_num1.DotColor = Color.Red;进行设置
            this.Owner.SetTypeColor(typeof(int), Color.Red);
        }
    }
}
