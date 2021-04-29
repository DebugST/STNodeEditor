using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
/*
MIT License

Copyright (c) 2021 DebugST@crystal_lz

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */
/*
 * create: 2021-12-08
 * modify: 2021-03-02
 * Author: Crystal_lz
 * blog: http://st233.com
 * Gitee: https://gitee.com/DebugST
 * Github: https://github.com/DebugST
 */
namespace ST.Library.UI.NodeEditor
{
    public class STNodeHub : STNode
    {
        private bool m_bSingle;
        private string m_strIn;
        private string m_strOut;

        public STNodeHub() : this(false, "IN", "OUT") { }
        public STNodeHub(bool bSingle) : this(bSingle, "IN", "OUT") { }
        public STNodeHub(bool bSingle, string strTextIn, string strTextOut) {
            m_bSingle = bSingle;
            m_strIn = strTextIn;
            m_strOut = strTextOut;
            this.Addhub();
            this.Title = "HUB";
            this.AutoSize = false;
            this.TitleColor = System.Drawing.Color.FromArgb(200, System.Drawing.Color.DarkOrange);
        }

        protected override void OnOwnerChanged() {
            base.OnOwnerChanged();
            if (this.Owner == null) return;
            using (Graphics g = this.Owner.CreateGraphics()) {
                this.Width = base.GetDefaultNodeSize(g).Width;
            }
        }

        private void Addhub() {
            var input = new STNodeHubOption(m_strIn, typeof(object), m_bSingle);
            var output = new STNodeHubOption(m_strOut, typeof(object), false);
            this.InputOptions.Add(input);
            this.OutputOptions.Add(output);
            input.Connected += new STNodeOptionEventHandler(input_Connected);
            input.DataTransfer += new STNodeOptionEventHandler(input_DataTransfer);
            input.DisConnected += new STNodeOptionEventHandler(input_DisConnected);
            output.Connected += new STNodeOptionEventHandler(output_Connected);
            output.DisConnected += new STNodeOptionEventHandler(output_DisConnected);
            this.Height = this.TitleHeight + this.InputOptions.Count * 20;
        }

        void output_DisConnected(object sender, STNodeOptionEventArgs e) {
            STNodeOption op = sender as STNodeOption;
            if (op.ConnectionCount != 0) return;
            int nIndex = this.OutputOptions.IndexOf(op);
            if (this.InputOptions[nIndex].ConnectionCount != 0) return;
            this.InputOptions.RemoveAt(nIndex);
            this.OutputOptions.RemoveAt(nIndex);
            if (this.Owner != null) this.Owner.BuildLinePath();
            this.Height -= 20;
        }

        void output_Connected(object sender, STNodeOptionEventArgs e) {
            STNodeOption op = sender as STNodeOption;
            int nIndex = this.OutputOptions.IndexOf(op);
            var t = typeof(object);
            if (this.InputOptions[nIndex].DataType == t) {
                op.DataType = e.TargetOption.DataType;
                this.InputOptions[nIndex].DataType = op.DataType;
                foreach (STNodeOption v in this.InputOptions) {
                    if (v.DataType == t) return;
                }
                this.Addhub();
            }
        }

        void input_DisConnected(object sender, STNodeOptionEventArgs e) {
            STNodeOption op = sender as STNodeOption;
            if (op.ConnectionCount != 0) return;
            int nIndex = this.InputOptions.IndexOf(op);
            if (this.OutputOptions[nIndex].ConnectionCount != 0) return;
            this.InputOptions.RemoveAt(nIndex);
            this.OutputOptions.RemoveAt(nIndex);
            if (this.Owner != null) this.Owner.BuildLinePath();
            this.Height -= 20;
        }

        void input_DataTransfer(object sender, STNodeOptionEventArgs e) {
            STNodeOption op = sender as STNodeOption;
            int nIndex = this.InputOptions.IndexOf(op);
            if (e.Status != ConnectionStatus.Connected)
                this.OutputOptions[nIndex].Data = null;
            else
                this.OutputOptions[nIndex].Data = e.TargetOption.Data;
            this.OutputOptions[nIndex].TransferData();
        }

        void input_Connected(object sender, STNodeOptionEventArgs e) {
            STNodeOption op = sender as STNodeOption;
            int nIndex = this.InputOptions.IndexOf(op);
            var t = typeof(object);
            if (op.DataType == t) {
                op.DataType = e.TargetOption.DataType;
                this.OutputOptions[nIndex].DataType = op.DataType;
                foreach (STNodeOption v in this.InputOptions) {
                    if (v.DataType == t) return;
                }
                this.Addhub();
            } else {
                //this.OutputOptions[nIndex].Data = e.TargetOption.Data;
                this.OutputOptions[nIndex].TransferData(e.TargetOption.Data);
            }
        }

        protected override void OnSaveNode(Dictionary<string, byte[]> dic) {
            dic.Add("count", BitConverter.GetBytes(this.InputOptionsCount));
            //dic.Add("single", new byte[] { (byte)(m_bSingle ? 1 : 0) });
            //dic.Add("strin", Encoding.UTF8.GetBytes(m_strIn));
            //dic.Add("strout", Encoding.UTF8.GetBytes(m_strOut));
        }

        protected internal override void OnLoadNode(Dictionary<string, byte[]> dic) {
            base.OnLoadNode(dic);
            int nCount = BitConverter.ToInt32(dic["count"], 0);
            while (this.InputOptionsCount < nCount && this.InputOptionsCount != nCount) this.Addhub();
            //m_bSingle = dic["single"][0] == 1;
            //m_strIn = Encoding.UTF8.GetString(dic["strin"]);
            //m_strOut = Encoding.UTF8.GetString(dic["strout"]);
        }

        public class STNodeHubOption : STNodeOption
        {
            public STNodeHubOption(string strText, Type dataType, bool bSingle) : base(strText, dataType, bSingle) { }

            public override ConnectionStatus ConnectOption(STNodeOption op) {
                var t = typeof(object);
                if (this.DataType != t) return base.ConnectOption(op);
                this.DataType = op.DataType;
                var ret = base.ConnectOption(op);
                if (ret != ConnectionStatus.Connected) this.DataType = t;
                return ret;
            }

            public override ConnectionStatus CanConnect(STNodeOption op) {
                if (op == STNodeOption.Empty) return ConnectionStatus.EmptyOption; 
                if (this.DataType != typeof(object)) return base.CanConnect(op);
                if (this.IsInput == op.IsInput) return ConnectionStatus.SameInputOrOutput;
                if (op.Owner == null || this.Owner == null) return ConnectionStatus.NoOwner;
                if (op.Owner == this.Owner) return ConnectionStatus.SameOwner;
                if (this.Owner.LockOption || op.Owner.LockOption) return ConnectionStatus.Locked;
                if (this.IsSingle && m_hs_connected.Count == 1) return ConnectionStatus.SingleOption;
                if (op.IsInput && STNodeEditor.CanFindNodePath(op.Owner, this.Owner)) return ConnectionStatus.Loop;
                if (m_hs_connected.Contains(op)) return ConnectionStatus.Exists;
                if (op.DataType == typeof(object)) return ConnectionStatus.ErrorType;

                if (!this.IsInput) return ConnectionStatus.Connected;
                foreach (STNodeOption owner_input in this.Owner.InputOptions) {
                    foreach (STNodeOption o in owner_input.ConnectedOption) {
                        if (o == op) return ConnectionStatus.Exists;
                    }
                }
                return ConnectionStatus.Connected; ;
            }
        }
    }
}
