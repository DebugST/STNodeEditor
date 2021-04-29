using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ST.Library.UI.NodeEditor;
using System.Drawing;

namespace WinNodeEditorDemo.NumberNode
{
    /// <summary>
    /// 此节点通过Number属性提供一个整数的输入
    /// </summary>
    [STNode("/Number","Crystal_lz","2212233137@qq.com","st233.com","Number input node")]
    public class NumberInputNode : NumberNode
    {
        private int _Number;
        [STNodeProperty("Input","this is input number")]
        public int Number {
            get { return _Number; }
            set { 
                _Number = value;
                m_op_number.TransferData(value); //将数据向下传递
                this.Invalidate();
            }
        }

        private STNodeOption m_op_number;       //输出选项
        private StringFormat m_sf = new StringFormat();

        protected override void OnCreate() {
            base.OnCreate();
            this.Title = "NumberInput";
            m_op_number = new STNodeOption("", typeof(int), false);
            this.OutputOptions.Add(m_op_number);
            m_sf = new StringFormat();
            m_sf.LineAlignment = StringAlignment.Center;
            m_sf.Alignment = StringAlignment.Far;
        }
        /// <summary>
        /// 当绘制选项文本时候 将数字绘制 因为STNodeOption.Text被protected修饰 STNode无法进行设置
        /// 因为作者并不建议对已经添加在STNode上的选项进行修改 尤其是在AutoSize被设置的情况下
        /// 若有需求 应当采用其他方式 比如:重绘 或者添加STNodeControl来显示变化的文本信息
        /// </summary>
        /// <param name="dt">绘制工具</param>
        /// <param name="op">需要绘制的选项</param>
        protected override void OnDrawOptionText(DrawingTools dt, STNodeOption op) {
            base.OnDrawOptionText(dt, op);
            dt.Graphics.DrawString(this._Number.ToString(), this.Font, Brushes.White, op.TextRectangle, m_sf);
        }
    }
}
