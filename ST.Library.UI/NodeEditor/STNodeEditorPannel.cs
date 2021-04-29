using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace ST.Library.UI.NodeEditor
{
    public class STNodeEditorPannel : Control
    {
        private bool _LeftLayout = true;
        /// <summary>
        /// 获取或设置是否是左边布局
        /// </summary>
        [Description("获取或设置是否是左边布局"), DefaultValue(true)]
        public bool LeftLayout {
            get { return _LeftLayout; }
            set {
                if (value == _LeftLayout) return;
                _LeftLayout = value;
                this.SetLocation();
                this.Invalidate();
            }
        }

        private Color _SplitLineColor = Color.Black;
        /// <summary>
        /// 获取或这是分割线颜色
        /// </summary>
        [Description("获取或这是分割线颜色"), DefaultValue(typeof(Color), "Black")]
        public Color SplitLineColor {
            get { return _SplitLineColor; }
            set { _SplitLineColor = value; }
        }

        private Color _HandleLineColor = Color.Gray;
        /// <summary>
        /// 获取或设置分割线手柄颜色
        /// </summary>
        [Description("获取或设置分割线手柄颜色"), DefaultValue(typeof(Color), "Gray")]
        public Color HandleLineColor {
            get { return _HandleLineColor; }
            set { _HandleLineColor = value; }
        }

        private bool _ShowScale = true;
        /// <summary>
        /// 获取或设置编辑器缩放时候显示比例
        /// </summary>
        [Description("获取或设置编辑器缩放时候显示比例"), DefaultValue(true)]
        public bool ShowScale {
            get { return _ShowScale; }
            set { _ShowScale = value; }
        }

        private bool _ShowConnectionStatus = true;
        /// <summary>
        /// 获取或设置节点连线时候是否显示状态
        /// </summary>
        [Description("获取或设置节点连线时候是否显示状态"), DefaultValue(true)]
        public bool ShowConnectionStatus {
            get { return _ShowConnectionStatus; }
            set { _ShowConnectionStatus = value; }
        }

        private int _X;
        /// <summary>
        /// 获取或设置分割线水平宽度
        /// </summary>
        [Description("获取或设置分割线水平宽度"), DefaultValue(201)]
        public int X {
            get { return _X; }
            set {
                if (value < 122) value = 122;
                else if (value > this.Width - 122) value = this.Width - 122;
                if (_X == value) return;
                _X = value;
                this.SetLocation();
            }
        }

        private int _Y;
        /// <summary>
        /// 获取或设置分割线垂直高度
        /// </summary>
        [Description("获取或设置分割线垂直高度")]
        public int Y {
            get { return _Y; }
            set {
                if (value < 122) value = 122;
                else if (value > this.Height - 122) value = this.Height - 122;
                if (_Y == value) return;
                _Y = value;
                this.SetLocation();
            }
        }
        /// <summary>
        /// 获取面板中的STNodeEditor
        /// </summary>
        [Description("获取面板中的STNodeEditor"), Browsable(false)]
        public STNodeEditor Editor {
            get { return m_editor; }
        }
        /// <summary>
        /// 获取面板中的STNodeTreeView
        /// </summary>
        [Description("获取面板中的STNodeTreeView"), Browsable(false)]
        public STNodeTreeView TreeView {
            get { return m_tree; }
        }
        /// <summary>
        /// 获取面板中的STNodePropertyGrid
        /// </summary>
        [Description("获取面板中的STNodePropertyGrid"), Browsable(false)]
        public STNodePropertyGrid PropertyGrid {
            get { return m_grid; }
        }

        private Point m_pt_down;
        private bool m_is_mx;
        private bool m_is_my;
        private Pen m_pen;

        private bool m_nInited;
        private Dictionary<ConnectionStatus, string> m_dic_status_key = new Dictionary<ConnectionStatus, string>();

        private STNodeEditor m_editor;
        private STNodeTreeView m_tree;
        private STNodePropertyGrid m_grid;

        public override Size MinimumSize {
            get {
                return base.MinimumSize;
            }
            set {
                value = new Size(250, 250);
                base.MinimumSize = value;
            }
        }

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool bRedraw);

        public STNodeEditorPannel() {
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);

            m_editor = new STNodeEditor();
            m_tree = new STNodeTreeView();
            m_grid = new STNodePropertyGrid();
            m_grid.Text = "NodeProperty";
            this.Controls.Add(m_editor);
            this.Controls.Add(m_tree);
            this.Controls.Add(m_grid);
            this.Size = new Size(500, 500);
            this.MinimumSize = new Size(250, 250);
            this.BackColor = Color.FromArgb(255, 34, 34, 34);

            m_pen = new Pen(this.BackColor, 3);

            Type t = typeof(ConnectionStatus);
            var vv = Enum.GetValues(t);
            var vvv = vv.GetValue(0);
            foreach (var f in t.GetFields()) {
                if (!f.FieldType.IsEnum) continue;
                foreach (var a in f.GetCustomAttributes(true)) {
                    if (!(a is DescriptionAttribute)) continue;
                    m_dic_status_key.Add((ConnectionStatus)f.GetValue(f), ((DescriptionAttribute)a).Description);
                }
            }

            m_editor.ActiveChanged += (s, e) => m_grid.SetNode(m_editor.ActiveNode);
            m_editor.CanvasScaled += (s, e) => {
                if (this._ShowScale)
                    m_editor.ShowAlert(m_editor.CanvasScale.ToString("F2"), Color.White, Color.FromArgb(127, 255, 255, 0));
            };
            m_editor.OptionConnected += (s, e) => {
                if (this._ShowConnectionStatus)
                    m_editor.ShowAlert(m_dic_status_key[e.Status], Color.White, e.Status == ConnectionStatus.Connected ? Color.FromArgb(125, Color.Lime) : Color.FromArgb(125, Color.Red));
            };
        }

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            if (!m_nInited) {
                this._Y = this.Height / 2;
                if (this._LeftLayout)
                    this._X = 201;
                else
                    this._X = this.Width - 202;
                m_nInited = true;
                this.SetLocation();
                return;
            }
            this.SetLocation();
        }

        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified) {
            if (width < 250) width = 250;
            if (height < 250) height = 250;
            base.SetBoundsCore(x, y, width, height, specified);
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            m_pen.Width = 3;
            m_pen.Color = this._SplitLineColor;
            g.DrawLine(m_pen, this._X, 0, this._X, this.Height);
            int nX = 0;
            if (this._LeftLayout) {
                g.DrawLine(m_pen, 0, this._Y, this._X - 1, this._Y);
                nX = this._X / 2;
            } else {
                g.DrawLine(m_pen, this._X + 2, this._Y, this.Width, this._Y);
                nX = this._X + (this.Width - this._X) / 2;
            }
            m_pen.Width = 1;
            this._HandleLineColor = Color.Gray;
            m_pen.Color = this._HandleLineColor;
            g.DrawLine(m_pen, this._X, this._Y - 10, this._X, this._Y + 10);
            g.DrawLine(m_pen, nX - 10, this._Y, nX + 10, this._Y);
        }

        private void SetLocation() {
            if (this._LeftLayout) {
                //m_tree.Location = Point.Empty;
                //m_tree.Size = new Size(m_sx - 1, m_sy - 1);
                STNodeEditorPannel.MoveWindow(m_tree.Handle, 0, 0, this._X - 1, this._Y - 1, false);

                //m_grid.Location = new Point(0, m_sy + 2);
                //m_grid.Size = new Size(m_sx - 1, this.Height - m_sy - 2);
                STNodeEditorPannel.MoveWindow(m_grid.Handle, 0, this._Y + 2, this._X - 1, this.Height - this._Y - 2, false);

                //m_editor.Location = new Point(m_sx + 2, 0);
                //m_editor.Size = new Size(this.Width - m_sx - 2, this.Height);
                STNodeEditorPannel.MoveWindow(m_editor.Handle, this._X + 2, 0, this.Width - this._X - 2, this.Height, false);
            } else {
                STNodeEditorPannel.MoveWindow(m_editor.Handle, 0, 0, this._X - 1, this.Height, false);
                STNodeEditorPannel.MoveWindow(m_tree.Handle, this._X + 2, 0, this.Width - this._X - 2, this._Y - 1, false);
                STNodeEditorPannel.MoveWindow(m_grid.Handle, this._X + 2, this._Y + 2, this.Width - this._X - 2, this.Height - this._Y - 2, false);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            m_pt_down = e.Location;
            m_is_mx = m_is_my = false;
            if (this.Cursor == Cursors.VSplit) {
                m_is_mx = true;
            } else if (this.Cursor == Cursors.HSplit) {
                m_is_my = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            if (e.Button == MouseButtons.Left) {
                int nw = 122;// (int)(this.Width * 0.1f);
                int nh = 122;// (int)(this.Height * 0.1f);
                if (m_is_mx) {
                    this._X = e.X;// -m_pt_down.X;
                    if (this._X < nw) this._X = nw;
                    else if (_X + nw > this.Width) this._X = this.Width - nw;
                } else if (m_is_my) {
                    this._Y = e.Y;
                    if (this._Y < nh) this._Y = nh;
                    else if (this._Y + nh > this.Height) this._Y = this.Height - nh;
                }
                //m_rx = this.Width - m_sx;// (float)m_sx / this.Width;
                //m_fh = (float)m_sy / this.Height;
                this.SetLocation();
                this.Invalidate();
                return;
            }
            if (Math.Abs(e.X - this._X) < 2)
                this.Cursor = Cursors.VSplit;
            else if (Math.Abs(e.Y - this._Y) < 2)
                this.Cursor = Cursors.HSplit;
            else this.Cursor = Cursors.Arrow;
        }

        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            m_is_mx = m_is_my = false;
            this.Cursor = Cursors.Arrow;
        }
        /// <summary>
        /// 向树控件中添加一个STNode
        /// </summary>
        /// <param name="stNodeType">STNode类型</param>
        /// <returns>是否添加成功</returns>
        public bool AddSTNode(Type stNodeType) {
            return m_tree.AddNode(stNodeType);
        }
        /// <summary>
        /// 从程序集中加载STNode
        /// </summary>
        /// <param name="strFileName">程序集路径</param>
        /// <returns>添加成功个数</returns>
        public int LoadAssembly(string strFileName) {
            m_editor.LoadAssembly(strFileName);
            return m_tree.LoadAssembly(strFileName);
        }
        /// <summary>
        /// 设置编辑器显示连接状态的文本
        /// </summary>
        /// <param name="status">连接状态</param>
        /// <param name="strText">对应显示文本</param>
        /// <returns>旧文本</returns>
        public string SetConnectionStatusText(ConnectionStatus status, string strText) {
            string strOld = null;
            if (m_dic_status_key.ContainsKey(status)) {
                strOld = m_dic_status_key[status];
                m_dic_status_key[status] = strText;
                return strOld;
            }
            m_dic_status_key.Add(status, strText);
            return strText;
        }
    }
}
