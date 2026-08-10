using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ST.Library.UI.NodeEditor;

namespace WinNodeEditorDemo
{
    /// <summary>
    /// 类库自带的STNodeHub并未被STNodeAttribute标记 无法被STNodeTreeView显示 所以需要扩展
    /// </summary>
    [STNode("/", "Crystal_lz", "2212233137@qq.com", "st233.com", "This is single Hub")]
    public class STNodeHubSingle : STNodeHub
    {
        public STNodeHubSingle()
            : base(true) {
            this.Title = "S_HUB";
        }
    }
    /// <summary>
    /// 类库自带的STNodeHub并未被STNodeAttribute标记 无法被STNodeTreeView显示 所以需要扩展
    /// </summary>
    [STNode("/", "Crystal_lz", "2212233137@qq.com", "st233.com", "This multi is Hub")]
    public class STNodeHubMulti : STNodeHub
    {
        public STNodeHubMulti()
            : base(false) {
            this.Title = "M_HUB";
        }
    }
}
