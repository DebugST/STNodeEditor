using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ST.Library.UI;

namespace ST.Library.UI
{
    public class DemoNode : STNode
    {
        protected override void OnCreate() {
            base.OnCreate();
            this.InputOptions.Add(new STNodeOption("Input", typeof(string), false));
            this.InputOptions.Add(new STNodeOption("SingleNode", typeof(System.Drawing.Image), true));
            this.InputOptions.Add(new STNodeOption("SingleNode", typeof(object), true));

            this.OutputOptions.Add(new STNodeOption("output", typeof(string), false));
            this.OutputOptions.Add(new STNodeOption("Single", typeof(System.Drawing.Icon), true));
            this.OutputOptions.Add(new STNodeOption("Single", typeof(object), true));

            this.Title = "Demo_Node";
        }
    }
}
