using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ST.Library.UI.NodeEditor;

namespace WinNodeEditorDemo
{
    [STNode("/")]
    public class EmptyOptionTestNode : STNode
    {
        protected override void OnCreate() {
            base.OnCreate();
            this.Title = "EmptyTest";
            this.InputOptions.Add(STNodeOption.Empty);
            this.InputOptions.Add("string", typeof(string), false);
        }
    }
}
