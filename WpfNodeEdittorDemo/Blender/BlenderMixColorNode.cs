using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ST.Library.UI.NodeEditor;
using System.Drawing;

namespace WinNodeEditorDemo.Blender
{
    /// <summary>
    /// 此类仅仅是演示 并不包含颜色混合功能
    /// </summary>
    [STNode("/Blender/", "Crystal_lz", "2212233137@qq.com", "st233.com", "this is blender mixrgb node")]
    public class BlenderMixColorNode : STNode
    {
        private ColorMixType _MixType;
        [STNodeProperty("MixType","This is MixType")]
        public ColorMixType MixType {
            get { return _MixType; }
            set { 
                _MixType = value;
                m_ctrl_select.Enum = value; //当属性被赋值后 对应控件状态值也应当被修改
            }
        }

        private bool _Clamp;
        [STNodeProperty("Clamp","This is Clamp")]
        public bool Clamp {
            get { return _Clamp; }
            set { _Clamp = value; m_ctrl_checkbox.Checked = value; }
        }

        private int _Fac = 50;
        [STNodeProperty("Fac", "This is Fac")]
        public int Fac {
            get { return _Fac; }
            set {
                if (value < 0) value = 0;
                if (value > 100) value = 100;
                _Fac = value; m_ctrl_progess.Value = value; 
            }
        }

        private Color _Color1 = Color.LightGray;//默认的DescriptorType不支持颜色的显示 需要扩展
        [STNodeProperty("Color1", "This is color1", DescriptorType = typeof(WinNodeEditorDemo.DescriptorForColor))]
        public Color Color1 {
            get { return _Color1; }
            set { _Color1 = value; m_ctrl_btn_1.BackColor = value; }
        }

        private Color _Color2 = Color.LightGray;
        [STNodeProperty("Color2", "This is color2", DescriptorType = typeof(WinNodeEditorDemo.DescriptorForColor))]
        public Color Color2 {
            get { return _Color2; }
            set { _Color2 = value; m_ctrl_btn_2.BackColor = value; }
        }

        public enum ColorMixType { 
            Mix,
            Value,
            Color,
            Hue,
            Add,
            Subtract
        }

        private STNodeSelectEnumBox m_ctrl_select;  //自定义控件
        private STNodeProgress m_ctrl_progess;
        private STNodeCheckBox m_ctrl_checkbox;
        private STNodeColorButton m_ctrl_btn_1;
        private STNodeColorButton m_ctrl_btn_2;

        protected override void OnCreate() {
            base.OnCreate();
            this.TitleColor = Color.FromArgb(200, Color.DarkKhaki);
            this.Title = "MixRGB";
            this.AutoSize = false;
            this.Size = new Size(140, 142);

            this.OutputOptions.Add("Color", typeof(Color), true);

            this.InputOptions.Add(STNodeOption.Empty);  //空白节点 仅站位 不参与绘制与事件触发
            this.InputOptions.Add(STNodeOption.Empty);
            this.InputOptions.Add(STNodeOption.Empty);
            this.InputOptions.Add("", typeof(float), true);
            this.InputOptions.Add("Color1", typeof(Color), true);
            this.InputOptions.Add("Color2", typeof(Color), true);

            m_ctrl_progess = new STNodeProgress();      //创建控件并添加到节点中
            m_ctrl_progess.Text = "Fac";
            m_ctrl_progess.DisplayRectangle = new Rectangle(10, 61, 120, 18);
            m_ctrl_progess.ValueChanged += (s, e) => this._Fac = m_ctrl_progess.Value;
            this.Controls.Add(m_ctrl_progess);

            m_ctrl_checkbox = new STNodeCheckBox();
            m_ctrl_checkbox.Text = "Clamp";
            m_ctrl_checkbox.DisplayRectangle = new Rectangle(10, 40, 120, 20);
            m_ctrl_checkbox.ValueChanged += (s, e) => this._Clamp = m_ctrl_checkbox.Checked;
            this.Controls.Add(m_ctrl_checkbox);

            m_ctrl_btn_1 = new STNodeColorButton();
            m_ctrl_btn_1.Text = "";
            m_ctrl_btn_1.BackColor = this._Color1;
            m_ctrl_btn_1.DisplayRectangle = new Rectangle(80, 82, 50, 16);
            m_ctrl_btn_1.ValueChanged += (s, e) => this._Color1 = m_ctrl_btn_1.BackColor;
            this.Controls.Add(m_ctrl_btn_1);

            m_ctrl_btn_2 = new STNodeColorButton();
            m_ctrl_btn_2.Text = "";
            m_ctrl_btn_2.BackColor = this._Color2;
            m_ctrl_btn_2.DisplayRectangle = new Rectangle(80, 102, 50, 16);
            m_ctrl_btn_2.ValueChanged += (s, e) => this._Color2 = m_ctrl_btn_2.BackColor;
            this.Controls.Add(m_ctrl_btn_2);

            m_ctrl_select = new STNodeSelectEnumBox();
            m_ctrl_select.DisplayRectangle = new Rectangle(10, 21, 120, 18);
            m_ctrl_select.Enum = this._MixType;
            m_ctrl_select.ValueChanged += (s, e) => this._MixType = (ColorMixType)m_ctrl_select.Enum;
            this.Controls.Add(m_ctrl_select);
        }

        protected override void OnOwnerChanged() {  //当控件被添加时候 向编辑器提交自己的数据类型希望显示的颜色
            base.OnOwnerChanged();
            if (this.Owner == null) return;
            this.Owner.SetTypeColor(typeof(float), Color.Gray);
            this.Owner.SetTypeColor(typeof(Color), Color.Yellow);
        }
    }
}
