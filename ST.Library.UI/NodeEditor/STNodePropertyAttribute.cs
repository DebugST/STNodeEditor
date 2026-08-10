using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace ST.Library.UI.NodeEditor
{
    /// <summary>
    /// STNode节点属性特性
    /// 用于描述STNode节点属性信息 以及在属性编辑器上的行为
    /// </summary>
    public class STNodePropertyAttribute : Attribute
    {
        private string _Name;
        /// <summary>
        /// 获取属性需要在属性编辑器上显示的名称
        /// </summary>
        public string Name {
            get { return _Name; }
        }

        private string _Description;
        /// <summary>
        /// 获取属性需要在属性编辑器上显示的描述
        /// </summary>
        public string Description {
            get { return _Description; }
        }

        private Type _ConverterType = typeof(STNodePropertyDescriptor);
        /// <summary>
        /// 获取属性描述器类型
        /// </summary>
        public Type DescriptorType {
            get { return _ConverterType; }
            set { _ConverterType = value; }
        }

        /// <summary>
        /// 构造一个STNode属性特性
        /// </summary>
        /// <param name="strKey">需要显示的名称</param>
        /// <param name="strDesc">需要显示的描述信息</param>
        public STNodePropertyAttribute(string strKey, string strDesc) {
            this._Name = strKey;
            this._Description = strDesc;
        }
        //private Type m_descriptor_type_base = typeof(STNodePropertyDescriptor);
    }
    /// <summary>
    /// STNode属性描述器
    /// 用于确定在属性编辑器上如何与属性的值进行交互 以及确定属性值在属性编辑器上将如何绘制并交互
    /// </summary>
    public class STNodePropertyDescriptor
    {
        /// <summary>
        /// 获取目标节点
        /// </summary>
        public STNode Node { get; internal set; }
        /// <summary>
        /// 获取所属的节点属性编辑器控件
        /// </summary>
        public STNodePropertyGrid Control { get; internal set; }
        /// <summary>
        /// 获取选项所在区域
        /// </summary>
        public Rectangle Rectangle { get; internal set; }
        /// <summary>
        /// 获取选项名称所在区域
        /// </summary>
        public Rectangle RectangleL { get; internal set; }
        /// <summary>
        /// 获取选项值所在区域
        /// </summary>
        public Rectangle RectangleR { get; internal set; }
        /// <summary>
        /// 获取选项需要显示的名称
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// 获取属性对应的描述信息
        /// </summary>
        public string Description { get; internal set; }
        /// <summary>
        /// 获取属性信息
        /// </summary>
        public PropertyInfo PropertyInfo { get; internal set; }

        private static Type m_t_int = typeof(int);
        private static Type m_t_float = typeof(float);
        private static Type m_t_double = typeof(double);
        private static Type m_t_string = typeof(string);
        private static Type m_t_bool = typeof(bool);

        private StringFormat m_sf;

        /// <summary>
        /// 构造一个描述器
        /// </summary>
        public STNodePropertyDescriptor() {
            m_sf = new StringFormat();
            m_sf.LineAlignment = StringAlignment.Center;
            m_sf.FormatFlags = StringFormatFlags.NoWrap;
        }

        /// <summary>
        /// 当确定STNode属性在属性编辑器上的位置时候发生
        /// </summary>
        protected internal virtual void OnSetItemLocation() { }
        /// <summary>
        /// 将字符串形式的属性值转换为属性目标类型的值
        /// 默认只支持 int float double string bool 以及上述类型的Array
        /// 若目标类型不在上述中 请重写此函数自行转换
        /// </summary>
        /// <param name="strText">字符串形式的属性值</param>
        /// <returns>属性真实目标类型的值</returns>
        protected internal virtual object GetValueFromString(string strText) {
            Type t = this.PropertyInfo.PropertyType;
            if (t == m_t_int) return int.Parse(strText);
            if (t == m_t_float) return float.Parse(strText);
            if (t == m_t_double) return double.Parse(strText);
            if (t == m_t_string) return strText;
            if (t == m_t_bool) return bool.Parse(strText);
            if (t.IsEnum) {
                return Enum.Parse(t, strText);
            } else if (t.IsArray) {
                var t_1 = t.GetElementType();
                if (t_1 == m_t_string) return strText.Split(',');
                string[] strs = strText.Trim(new char[] { ' ', ',' }).Split(',');//add other place trim()
                if (t_1 == m_t_int) {
                    int[] arr = new int[strs.Length];
                    for (int i = 0; i < strs.Length; i++) arr[i] = int.Parse(strs[i].Trim());
                    return arr;
                }
                if (t_1 == m_t_float) {
                    float[] arr = new float[strs.Length];
                    for (int i = 0; i < strs.Length; i++) arr[i] = float.Parse(strs[i].Trim());
                    return arr;
                }
                if (t_1 == m_t_int) {
                    double[] arr = new double[strs.Length];
                    for (int i = 0; i < strs.Length; i++) arr[i] = double.Parse(strs[i].Trim());
                    return arr;
                }
                if (t_1 == m_t_int) {
                    bool[] arr = new bool[strs.Length];
                    for (int i = 0; i < strs.Length; i++) arr[i] = bool.Parse(strs[i].Trim());
                    return arr;
                }
            }
            throw new InvalidCastException("无法完成[string]到[" + t.FullName + "]的转换 请重载[STNodePropertyDescriptor.GetValueFromString(string)]");
        }
        /// <summary>
        /// 将属性目标类型的值转换为字符串形式的值
        /// 默认对类型值进行 ToString() 操作
        /// 如需特殊处理 请重写此函数自行转换
        /// </summary>
        /// <returns>属性值的字符串形式</returns>
        protected internal virtual string GetStringFromValue() {
            var v = this.PropertyInfo.GetValue(this.Node, null);
            var t = this.PropertyInfo.PropertyType;
            if (v == null) return null;
            if (t.IsArray) {
                List<string> lst = new List<string>();
                foreach (var item in (Array)v) lst.Add(item.ToString());
                return string.Join(",", lst.ToArray());
            }
            return v.ToString();
        }
        /// <summary>
        /// 将二进制形式的属性值转换为属性目标类型的值 用于从文件存储中的数据还原属性值
        /// 默认将其转换为字符串然后调用 GetValueFromString(string)
        /// 此函数与 GetBytesFromValue() 相对应 若需要重写函数应当两个函数一起重写
        /// </summary>
        /// <param name="byData">二进制数据</param>
        /// <returns>属性真实目标类型的值</returns>
        protected internal virtual object GetValueFromBytes(byte[] byData) {
            if (byData == null) return null;
            string strText = Encoding.UTF8.GetString(byData);
            return this.GetValueFromString(strText);
        }
        /// <summary>
        /// 将属性目标类型的值转换为二进制形式的值 用于文件存储时候调用
        /// 默认调用 GetStringFromValue() 然后将字符串转换为二进制数据
        /// 如需特殊处理 请重写此函数自行转换 并且重写 GetValueFromBytes()
        /// </summary>
        /// <returns>属性值的二进制形式</returns>
        protected internal virtual byte[] GetBytesFromValue() {
            string strText = this.GetStringFromValue();
            if (strText == null) return null;
            return Encoding.UTF8.GetBytes(strText);
        }
        /// <summary>
        /// 此函数对应 System.Reflection.PropertyInfo.GetValue()
        /// </summary>
        /// <param name="index">索引属性的可选索引值 对于非索引属性 此值应为null</param>
        /// <returns>属性值</returns>
        protected internal virtual object GetValue(object[] index) {
            return this.PropertyInfo.GetValue(this.Node, index);
        }
        /// <summary>
        /// 此函数对应 System.Reflection.PropertyInfo.SetValue()
        /// </summary>
        /// <param name="value">需要设置的属性值</param>
        protected internal virtual void SetValue(object value) {
            this.PropertyInfo.SetValue(this.Node, value, null);
        }
        /// <summary>
        /// 此函数对应 System.Reflection.PropertyInfo.SetValue()
        /// 在调用之前会默认进行 GetValueFromString(strValue) 处理
        /// </summary>
        /// <param name="strValue">需要设置的属性字符串形式的值</param>
        protected internal virtual void SetValue(string strValue) {
            this.PropertyInfo.SetValue(this.Node, this.GetValueFromString(strValue), null);
        }
        /// <summary>
        /// 此函数对应 System.Reflection.PropertyInfo.SetValue()
        /// 在调用之前会默认进行 GetValueFromBytes(byte[]) 处理
        /// </summary>
        /// <param name="byData">需要设置的属性二进制数据</param>
        protected internal virtual void SetValue(byte[] byData) {
            this.PropertyInfo.SetValue(this.Node, this.GetValueFromBytes(byData), null);
        }
        /// <summary>
        /// 此函数对应 System.Reflection.PropertyInfo.SetValue()
        /// </summary>
        /// <param name="value">需要设置的属性值</param>
        /// <param name="index">索引属性的可选索引值 对于非索引属性 此值应为null</param>
        protected internal virtual void SetValue(object value, object[] index) {
            this.PropertyInfo.SetValue(this.Node, value, index);
        }
        /// <summary>
        /// 此函数对应 System.Reflection.PropertyInfo.SetValue()
        /// 在调用之前会默认进行 GetValueFromString(strValue) 处理
        /// </summary>
        /// <param name="strValue">需要设置的属性字符串形式的值</param>
        /// <param name="index">索引属性的可选索引值 对于非索引属性 此值应为null</param>
        protected internal virtual void SetValue(string strValue, object[] index) {
            this.PropertyInfo.SetValue(this.Node, this.GetValueFromString(strValue), index);
        }
        /// <summary>
        /// 此函数对应 System.Reflection.PropertyInfo.SetValue()
        /// 在调用之前会默认进行 GetValueFromBytes(byte[]) 处理
        /// </summary>
        /// <param name="byData">需要设置的属性二进制数据</param>
        /// <param name="index">索引属性的可选索引值 对于非索引属性 此值应为null</param>
        protected internal virtual void SetValue(byte[] byData, object[] index) {
            this.PropertyInfo.SetValue(this.Node, this.GetValueFromBytes(byData), index);
        }
        /// <summary>
        /// 当设置属性值发生错误时候发生
        /// </summary>
        /// <param name="ex">异常信息</param>
        protected internal virtual void OnSetValueError(Exception ex) {
            this.Control.SetErrorMessage(ex.Message);
        }
        /// <summary>
        /// 当绘制属性在属性编辑器上的值所在区域时候发生
        /// </summary>
        /// <param name="dt">绘制工具</param>
        protected internal virtual void OnDrawValueRectangle(DrawingTools dt) {
            Graphics g = dt.Graphics;
            SolidBrush brush = dt.SolidBrush;
            STNodePropertyGrid ctrl = this.Control;
            //STNodePropertyItem item = this._PropertyItem;
            brush.Color = ctrl.ItemValueBackColor;

            g.FillRectangle(brush, this.RectangleR);
            Rectangle rect = this.RectangleR;
            rect.Width--; rect.Height--;
            brush.Color = this.Control.ForeColor;
            g.DrawString(this.GetStringFromValue(), ctrl.Font, brush, this.RectangleR, m_sf);

            if (this.PropertyInfo.PropertyType.IsEnum || this.PropertyInfo.PropertyType == m_t_bool) {
                g.FillPolygon(Brushes.Gray, new Point[]{
                        new Point(rect.Right - 13, rect.Top + rect.Height / 2 - 2),
                        new Point(rect.Right - 4, rect.Top + rect.Height / 2 - 2),
                        new Point(rect.Right - 9, rect.Top + rect.Height / 2 + 3)
                    });
            }
        }
        /// <summary>
        /// 当鼠标进入属性值所在区域时候发生
        /// </summary>
        /// <param name="e">事件参数</param>
        protected internal virtual void OnMouseEnter(EventArgs e) { }
        /// <summary>
        /// 当鼠标在属性值所在区域点击时候发生
        /// </summary>
        /// <param name="e">事件参数</param>
        protected internal virtual void OnMouseDown(MouseEventArgs e) {
        }
        /// <summary>
        /// 当鼠标在属性值所在区域移动时候发生
        /// </summary>
        /// <param name="e">事件参数</param>
        protected internal virtual void OnMouseMove(MouseEventArgs e) { }
        /// <summary>
        /// 当鼠标在属性值所在区域抬起时候发生
        /// </summary>
        /// <param name="e">事件参数</param>
        protected internal virtual void OnMouseUp(MouseEventArgs e) { }
        /// <summary>
        /// 当鼠标在属性值所在区域离开时候发生
        /// </summary>
        /// <param name="e">事件参数</param>
        protected internal virtual void OnMouseLeave(EventArgs e) { }
        /// <summary>
        /// 当鼠标在属性值所在区域点击时候发生
        /// </summary>
        /// <param name="e">事件参数</param>
        protected internal virtual void OnMouseClick(MouseEventArgs e) {
            Type t = this.PropertyInfo.PropertyType;
            if (t == m_t_bool || t.IsEnum) {
                new FrmSTNodePropertySelect(this).Show(this.Control);
                return;
            }
            Rectangle rect = this.Control.RectangleToScreen(this.RectangleR);
            new FrmSTNodePropertyInput(this).Show(this.Control);
        }
        /// <summary>
        /// 重绘选项区域
        /// </summary>
        public void Invalidate() {
            Rectangle rect = this.Rectangle;
            rect.X -= this.Control.ScrollOffset;
            this.Control.Invalidate(rect);
        }
    }
}
