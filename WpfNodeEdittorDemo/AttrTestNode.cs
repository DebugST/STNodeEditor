using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ST.Library.UI.NodeEditor;
using System.Drawing;
using System.Windows.Forms;

namespace WinNodeEditorDemo
{
    [STNode("/", "Crystal_lz", "2212233137@qq.com", "www.st233.com", "关于此节点的描述信息\r\n此类为\r\nSTNodeAttribute\r\nSTNodePropertyAttribute\r\n效果演示类")]
    public class AttrTestNode : STNode
    {
        //因为属性编辑器默认并不支持Color类型数据 所以这里重写一个描述器并指定
        [STNodeProperty("颜色", "颜色信息", DescriptorType = typeof(DescriptorForColor))]
        public Color Color { get; set; }

        [STNodeProperty("整型数组", "整型数组测试")]
        public int[] IntArr { get; set; }

        [STNodeProperty("布尔", "布尔类型测试")]
        public bool Bool { get; set; }

        [STNodeProperty("字符串", "字符串类型测试")]
        public string String { get; set; }

        [STNodeProperty("整型", "整型测试")]
        public int Int { get; set; }

        [STNodeProperty("浮点数", "浮点数类型测试")]
        public float Float { get; set; }

        [STNodeProperty("枚举值", "枚举类型测试 -> FormBorderStyle")]
        public FormBorderStyle STYLE { get; set; }

        public AttrTestNode() {
            this.String = "string";
            IntArr = new int[] { 10, 20 };
            base.InputOptions.Add("string", typeof(string), false);
            base.OutputOptions.Add("string", typeof(string), false);
            this.Title = "AttrTestNode";
            this.TitleColor = Color.FromArgb(200, Color.Goldenrod);
        }
        /// <summary>
        /// 此方法为魔术方法(Magic function)
        /// 若存在 static void ShowHelpInfo(string) 且此类被STNodeAttribute标记
        /// 则此方法将作为属性编辑器上 查看帮助 功能
        /// </summary>
        /// <param name="strFileName">此类所在的模块所在的文件路径</param>
        public static void ShowHelpInfo(string strFileName) {
            MessageBox.Show("this is -> ShowHelpInfo(string);\r\n" + strFileName);
        }

        protected override void OnOwnerChanged() {
            base.OnOwnerChanged();
            if (this.Owner == null) return;
            this.Owner.SetTypeColor(typeof(string), Color.Goldenrod);
        }
    }
    /// <summary>
    /// 因为属性编辑器默认并不支持Color类型数据 所以这里重写一个描述器
    /// </summary>
    public class DescriptorForColor : STNodePropertyDescriptor
    {
        private Rectangle m_rect;//此区域用作 属性窗口上绘制颜色预览
        //当此属性在属性窗口中被确定位置时候发生
        protected override void OnSetItemLocation() {
            base.OnSetItemLocation();
            Rectangle rect = base.RectangleR;
            m_rect = new Rectangle(rect.Right - 25, rect.Top + 5, 19, 12);
        }
        //将属性值转换为字符串 属性窗口值绘制时将采用此字符串
        protected override string GetStringFromValue() {
            Color clr = (Color)this.GetValue(null);
            return clr.A + "," + clr.R + "," + clr.G + "," + clr.B;
        }
        //将属性窗口中输入的字符串转化为Color属性 当属性窗口中用户确认输入时调用
        protected override object GetValueFromString(string strText) {
            string[] strClr = strText.Split(',');
            return Color.FromArgb(
                int.Parse(strClr[0]),   //A
                int.Parse(strClr[1]),   //R
                int.Parse(strClr[2]),   //G
                int.Parse(strClr[3]));  //B
        }
        //绘制属性窗口值区域时候调用
        protected override void OnDrawValueRectangle(DrawingTools dt) {
            base.OnDrawValueRectangle(dt);//先采用默认的绘制 并再绘制颜色预览
            dt.SolidBrush.Color = (Color)this.GetValue(null);
            dt.Graphics.FillRectangle(dt.SolidBrush, m_rect);//填充颜色
            dt.Graphics.DrawRectangle(Pens.Black, m_rect);   //绘制边框
        }

        protected override void OnMouseClick(MouseEventArgs e) {
            //如果用户点击在 颜色预览区域 则弹出系统颜色对话框
            if (m_rect.Contains(e.Location)) {
                ColorDialog cd = new ColorDialog();
                if (cd.ShowDialog() != DialogResult.OK) return;
                this.SetValue(cd.Color, null);
                this.Invalidate();
                return;
            }
            //否则其他区域将采用默认处理方式 弹出字符串输入框
            base.OnMouseClick(e);
        }
    }
}
