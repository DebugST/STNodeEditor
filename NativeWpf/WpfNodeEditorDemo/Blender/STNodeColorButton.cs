using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;
using ST.Library.UI.NodeEditor;
using WpfNodeEditorDemo;

namespace WinNodeEditorDemo.Blender
{
    /// <summary>
    /// 此类仅演示 作为MixRGB节点的颜色选择按钮
    /// </summary>
    public class STNodeColorButton : STNodeControl
    {
        public event EventHandler ValueChanged;
        protected virtual void OnValueChanged(EventArgs e) {
            if (this.ValueChanged != null) this.ValueChanged(this, e);
        }

        protected override void OnMouseClick(STNodeMouseEventArgs e) {
            base.OnMouseClick(e);
            if (!WpfColorPicker.TryPick(this.BackColor, out Color selected)) return;
            this.BackColor = selected;
            this.OnValueChanged(new EventArgs());
        }
    }
}
