using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;
using System.Windows.Forms;
using System.Collections;
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
    public abstract class STNode
    {
        private STNodeEditor _Owner;
        /// <summary>
        /// 获取当前 Node 所有者
        /// </summary>
        public STNodeEditor Owner {
            get { return _Owner; }
            internal set {
                if (value == _Owner) return;
                if (_Owner != null) {
                    foreach (STNodeOption op in this._InputOptions.ToArray()) op.DisConnectionAll();
                    foreach (STNodeOption op in this._OutputOptions.ToArray()) op.DisConnectionAll();
                }
                _Owner = value;
                if (!this._AutoSize) this.SetOptionsLocation();
                this.BuildSize(true, true, false);
                this.OnOwnerChanged();
            }
        }

        private bool _IsSelected;
        /// <summary>
        /// 获取或设置 Node 是否处于被选中状态
        /// </summary>
        public bool IsSelected {
            get { return _IsSelected; }
            set {
                if (value == _IsSelected) return;
                _IsSelected = value;
                this.Invalidate();
                this.OnSelectedChanged();
                if (this._Owner != null) this._Owner.OnSelectedChanged(EventArgs.Empty);
            }
        }

        private bool _IsActive;
        /// <summary>
        /// 获取 Node 是否处于活动状态
        /// </summary>
        public bool IsActive {
            get { return _IsActive; }
            internal set {
                if (value == _IsActive) return;
                _IsActive = value;
                this.OnActiveChanged();
            }
        }

        private Color _TitleColor;
        /// <summary>
        /// 获取或设置标题背景颜色
        /// </summary>
        public Color TitleColor {
            get { return _TitleColor; }
            protected set {
                _TitleColor = value;
                this.Invalidate(new Rectangle(0, 0, this._Width, this._TitleHeight));
            }
        }

        private Color _MarkColor;
        /// <summary>
        /// 获取或设置标记信息背景颜色
        /// </summary>
        public Color MarkColor {
            get { return _MarkColor; }
            protected set {
                _MarkColor = value;
                this.Invalidate(this._MarkRectangle);
            }
        }

        private Color _ForeColor = Color.White;
        /// <summary>
        /// 获取或设置当前 Node 前景色
        /// </summary>
        public Color ForeColor {
            get { return _ForeColor; }
            protected set {
                _ForeColor = value;
                this.Invalidate();
            }
        }

        private Color _BackColor;
        /// <summary>
        /// 获取或设置当前 Node 背景色
        /// </summary>
        public Color BackColor {
            get { return _BackColor; }
            protected set {
                _BackColor = value;
                this.Invalidate();
            }
        }

        private string _Title;
        /// <summary>
        /// 获取或设置 Node 标题
        /// </summary>
        public string Title {
            get { return _Title; }
            protected set {
                _Title = value;
                if (this._AutoSize) this.BuildSize(true, true, true);
                //this.Invalidate(this.TitleRectangle);
            }
        }

        private string _Mark;
        /// <summary>
        /// 获取或设置 Node 标记信息
        /// </summary>
        public string Mark {
            get { return _Mark; }
            set {
                _Mark = value;
                if (value == null)
                    _MarkLines = null;
                else
                    _MarkLines = (from s in value.Split('\n') select s.Trim()).ToArray();
                this.Invalidate(new Rectangle(-5, -5, this._MarkRectangle.Width + 10, this._MarkRectangle.Height + 10));
            }
        }

        private string[] _MarkLines;//单独存放行数据 不用每次在绘制中去拆分
        /// <summary>
        /// 获取 Node 标记信息行数据
        /// </summary>
        public string[] MarkLines {
            get { return _MarkLines; }
        }

        private int _Left;
        /// <summary>
        /// 获取或设置 Node 左边坐标
        /// </summary>
        public int Left {
            get { return _Left; }
            set {
                if (this._LockLocation || value == _Left) return;
                _Left = value;
                this.SetOptionsLocation();
                this.BuildSize(false, true, false);
                this.OnMove(EventArgs.Empty);
                if (this._Owner != null) {
                    this._Owner.BuildLinePath();
                    this._Owner.BuildBounds();
                }
            }
        }

        private int _Top;
        /// <summary>
        /// 获取或设置 Node 上边坐标
        /// </summary>
        public int Top {
            get { return _Top; }
            set {
                if (this._LockLocation || value == _Top) return;
                _Top = value;
                this.SetOptionsLocation();
                this.BuildSize(false, true, false);
                this.OnMove(EventArgs.Empty);
                if (this._Owner != null) {
                    this._Owner.BuildLinePath();
                    this._Owner.BuildBounds();
                }
            }
        }

        private int _Width = 100;
        /// <summary>
        /// 获取或设置 Node 宽度 当AutoSize被设置时 无法设置此值
        /// </summary>
        public int Width {
            get { return _Width; }
            protected set {
                if (value < 50) return;
                if (this._AutoSize || value == _Width) return;
                _Width = value;
                this.SetOptionsLocation();
                this.BuildSize(false, true, false);
                this.OnResize(EventArgs.Empty);
                if (this._Owner != null) {
                    this._Owner.BuildLinePath();
                    this._Owner.BuildBounds();
                }
                this.Invalidate();
            }
        }

        private int _Height = 40;
        /// <summary>
        /// 获取或设置 Node 高度 当AutoSize被设置时 无法设置此值
        /// </summary>
        public int Height {
            get { return _Height; }
            protected set {
                if (value < 40) return;
                if (this._AutoSize || value == _Height) return;
                _Height = value;
                this.SetOptionsLocation();
                this.BuildSize(false, true, false);
                this.OnResize(EventArgs.Empty);
                if (this._Owner != null) {
                    this._Owner.BuildLinePath();
                    this._Owner.BuildBounds();
                }
                this.Invalidate();
            }
        }

        private int _ItemHeight = 20;
        /// <summary>
        /// 获取或设置 Node 每个选项的高度
        /// </summary>
        public int ItemHeight {
            get { return _ItemHeight; }
            protected set {
                if (value < 16) value = 16;
                if (value > 200) value = 200;
                if (value == _ItemHeight) return;
                _ItemHeight = value;
                if (this._AutoSize) {
                    this.BuildSize(true, false, true);
                } else {
                    this.SetOptionsLocation();
                    if (this._Owner != null) this._Owner.Invalidate();
                }
            }
        }

        private bool _AutoSize = true;
        /// <summary>
        /// 获取或设置 Node 是否自动计算宽高
        /// </summary>
        public bool AutoSize {
            get { return _AutoSize; }
            protected set { _AutoSize = value; }
        }
        /// <summary>
        /// 获取 Node 右边边坐标
        /// </summary>
        public int Right {
            get { return _Left + _Width; }
        }
        /// <summary>
        /// 获取 Node 下边坐标
        /// </summary>
        public int Bottom {
            get { return _Top + _Height; }
        }
        /// <summary>
        /// 获取 Node 矩形区域
        /// </summary>
        public Rectangle Rectangle {
            get {
                return new Rectangle(this._Left, this._Top, this._Width, this._Height);
            }
        }
        /// <summary>
        /// 获取 Node 标题矩形区域
        /// </summary>
        public Rectangle TitleRectangle {
            get {
                return new Rectangle(this._Left, this._Top, this._Width, this._TitleHeight);
            }
        }

        private Rectangle _MarkRectangle;
        /// <summary>
        /// 获取 Node 标记矩形区域
        /// </summary>
        public Rectangle MarkRectangle {
            get { return _MarkRectangle; }
        }

        private int _TitleHeight = 20;
        /// <summary>
        /// 获取或设置 Node 标题高度
        /// </summary>
        public int TitleHeight {
            get { return _TitleHeight; }
            protected set { _TitleHeight = value; }
        }

        private STNodeOptionCollection _InputOptions;
        /// <summary>
        /// 获取输入选项集合
        /// </summary>
        protected internal STNodeOptionCollection InputOptions {
            get { return _InputOptions; }
        }
        /// <summary>
        /// 获取输入选项集合个数
        /// </summary>
        public int InputOptionsCount { get { return _InputOptions.Count; } }

        private STNodeOptionCollection _OutputOptions;
        /// <summary>
        /// 获取输出选项
        /// </summary>
        protected internal STNodeOptionCollection OutputOptions {
            get { return _OutputOptions; }
        }
        /// <summary>
        /// 获取输出选项个数
        /// </summary>
        public int OutputOptionsCount { get { return _OutputOptions.Count; } }

        private STNodeControlCollection _Controls;
        /// <summary>
        /// 获取 Node 所包含的控件集合
        /// </summary>
        protected STNodeControlCollection Controls {
            get { return _Controls; }
        }
        /// <summary>
        /// 获取 Node 所包含的控件集合个数
        /// </summary>
        public int ControlsCount { get { return _Controls.Count; } }
        /// <summary>
        /// 获取 Node 坐标位置
        /// </summary>
        public Point Location {
            get { return new Point(this._Left, this._Top); }
            set {
                this.Left = value.X;
                this.Top = value.Y;
            }
        }
        /// <summary>
        /// 获取 Node 大小
        /// </summary>
        public Size Size {
            get { return new Size(this._Width, this._Height); }
            set {
                this.Width = value.Width;
                this.Height = value.Height;
            }
        }

        private Font _Font;
        /// <summary>
        /// 获取或设置 Node 字体
        /// </summary>
        protected Font Font {
            get { return _Font; }
            set {
                if (value == _Font) return;
                this._Font.Dispose();
                _Font = value;
            }
        }

        private bool _LockOption;
        /// <summary>
        /// 获取或设置是否锁定Option选项 锁定后不在接受连接
        /// </summary>
        public bool LockOption {
            get { return _LockOption; }
            set {
                _LockOption = value;
                this.Invalidate(new Rectangle(0, 0, this._Width, this._TitleHeight));
            }
        }

        private bool _LockLocation;
        /// <summary>
        /// 获取或设置是否锁定Node位置 锁定后不可移动
        /// </summary>
        public bool LockLocation {
            get { return _LockLocation; }
            set {
                _LockLocation = value;
                this.Invalidate(new Rectangle(0, 0, this._Width, this._TitleHeight));
            }
        }

        private ContextMenuStrip _ContextMenuStrip;
        /// <summary>
        /// 获取或设置当前Node 上下文菜单
        /// </summary>
        public ContextMenuStrip ContextMenuStrip {
            get { return _ContextMenuStrip; }
            set { _ContextMenuStrip = value; }
        }

        private object _Tag;
        /// <summary>
        /// 获取或设置用户自定义保存的数据
        /// </summary>
        public object Tag {
            get { return _Tag; }
            set { _Tag = value; }
        }

        private Guid _Guid;
        /// <summary>
        /// 获取全局唯一标识
        /// </summary>
        public Guid Guid {
            get { return _Guid; }
        }

        private bool _LetGetOptions = false;
        /// <summary>
        /// 获取或设置是否允许外部访问STNodeOption
        /// </summary>
        public bool LetGetOptions {
            get { return _LetGetOptions; }
            protected set { _LetGetOptions = value; }
        }

        private static Point m_static_pt_init = new Point(10, 10);

        public STNode() {
            this._Title = "Untitled";
            this._MarkRectangle.Height = this._Height;
            this._Left = this._MarkRectangle.X = m_static_pt_init.X;
            this._Top = m_static_pt_init.Y;
            this._MarkRectangle.Y = this._Top - 30;
            this._InputOptions = new STNodeOptionCollection(this, true);
            this._OutputOptions = new STNodeOptionCollection(this, false);
            this._Controls = new STNodeControlCollection(this);
            this._BackColor = Color.FromArgb(200, 64, 64, 64);
            this._TitleColor = Color.FromArgb(200, Color.DodgerBlue);
            this._MarkColor = Color.FromArgb(200, Color.Brown);
            this._Font = new Font("courier new", 8.25f);

            m_sf = new StringFormat();
            m_sf.Alignment = StringAlignment.Near;
            m_sf.LineAlignment = StringAlignment.Center;
            m_sf.FormatFlags = StringFormatFlags.NoWrap;
            m_sf.SetTabStops(0, new float[] { 40 });
            m_static_pt_init.X += 10;
            m_static_pt_init.Y += 10;
            this._Guid = Guid.NewGuid();
            this.OnCreate();
        }

        //private int m_nItemHeight = 30;
        protected StringFormat m_sf;
        /// <summary>
        /// 当前Node中 活动的控件
        /// </summary>
        protected STNodeControl m_ctrl_active;
        /// <summary>
        /// 当前Node中 悬停的控件
        /// </summary>
        protected STNodeControl m_ctrl_hover;
        /// <summary>
        /// 当前Node中 鼠标点下的控件
        /// </summary>
        protected STNodeControl m_ctrl_down;

        protected internal void BuildSize(bool bBuildNode, bool bBuildMark, bool bRedraw) {
            if (this._Owner == null) return;
            using (Graphics g = this._Owner.CreateGraphics()) {
                if (this._AutoSize && bBuildNode) {
                    Size sz = this.GetDefaultNodeSize(g);
                    if (this._Width != sz.Width || this._Height != sz.Height) {
                        this._Width = sz.Width;
                        this._Height = sz.Height;
                        this.SetOptionsLocation();
                        this.OnResize(EventArgs.Empty);
                    }
                }
                if (bBuildMark && !string.IsNullOrEmpty(this._Mark)) {
                    this._MarkRectangle = this.OnBuildMarkRectangle(g);
                }
            }
            if (bRedraw) this._Owner.Invalidate();
        }

        internal Dictionary<string, byte[]> OnSaveNode() {
            Dictionary<string, byte[]> dic = new Dictionary<string, byte[]>();
            dic.Add("Guid", this._Guid.ToByteArray());
            dic.Add("Left", BitConverter.GetBytes(this._Left));
            dic.Add("Top", BitConverter.GetBytes(this._Top));
            dic.Add("Width", BitConverter.GetBytes(this._Width));
            dic.Add("Height", BitConverter.GetBytes(this._Height));
            dic.Add("AutoSize", new byte[] { (byte)(this._AutoSize ? 1 : 0) });
            if (this._Mark != null) dic.Add("Mark", Encoding.UTF8.GetBytes(this._Mark));
            dic.Add("LockOption", new byte[] { (byte)(this._LockLocation ? 1 : 0) });
            dic.Add("LockLocation", new byte[] { (byte)(this._LockLocation ? 1 : 0) });
            Type t = this.GetType();
            foreach (var p in t.GetProperties()) {
                var attrs = p.GetCustomAttributes(true);
                foreach (var a in attrs) {
                    if (!(a is STNodePropertyAttribute)) continue;
                    var attr = a as STNodePropertyAttribute;
                    object obj = Activator.CreateInstance(attr.DescriptorType);
                    if (!(obj is STNodePropertyDescriptor))
                        throw new InvalidOperationException("[STNodePropertyAttribute.Type]参数值必须为[STNodePropertyDescriptor]或者其子类的类型");
                    var desc = (STNodePropertyDescriptor)Activator.CreateInstance(attr.DescriptorType);
                    desc.Node = this;
                    desc.PropertyInfo = p;
                    byte[] byData = desc.GetBytesFromValue();
                    if (byData == null) continue;
                    dic.Add(p.Name, byData);
                }
            }
            this.OnSaveNode(dic);
            return dic;
        }

        internal byte[] GetSaveData() {
            List<byte> lst = new List<byte>();
            Type t = this.GetType();
            byte[] byData = Encoding.UTF8.GetBytes(t.Module.Name + "|" + t.FullName);
            lst.Add((byte)byData.Length);
            lst.AddRange(byData);
            byData = Encoding.UTF8.GetBytes(t.GUID.ToString());
            lst.Add((byte)byData.Length);
            lst.AddRange(byData);

            var dic = this.OnSaveNode();
            if (dic != null) {
                foreach (var v in dic) {
                    byData = Encoding.UTF8.GetBytes(v.Key);
                    lst.AddRange(BitConverter.GetBytes(byData.Length));
                    lst.AddRange(byData);
                    lst.AddRange(BitConverter.GetBytes(v.Value.Length));
                    lst.AddRange(v.Value);
                }
            }
            return lst.ToArray();
        }

        #region protected
        /// <summary>
        /// 当Node被构造时候发生
        /// </summary>
        protected virtual void OnCreate() { }
        /// <summary>
        /// 绘制整个Node
        /// </summary>
        /// <param name="dt">绘制工具</param>
        protected internal virtual void OnDrawNode(DrawingTools dt) {
            dt.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            //Fill background
            if (this._BackColor.A != 0) {
                dt.SolidBrush.Color = this._BackColor;
                dt.Graphics.FillRectangle(dt.SolidBrush, this._Left, this._Top + this._TitleHeight, this._Width, this.Height - this._TitleHeight);
            }
            this.OnDrawTitle(dt);
            this.OnDrawBody(dt);
        }
        /// <summary>
        /// 绘制Node标题部分
        /// </summary>
        /// <param name="dt">绘制工具</param>
        protected virtual void OnDrawTitle(DrawingTools dt) {
            m_sf.Alignment = StringAlignment.Center;
            m_sf.LineAlignment = StringAlignment.Center;
            Graphics g = dt.Graphics;
            SolidBrush brush = dt.SolidBrush;
            if (this._TitleColor.A != 0) {
                brush.Color = this._TitleColor;
                g.FillRectangle(brush, this.TitleRectangle);
            }
            if (this._LockOption) {
                //dt.Pen.Color = this.ForeColor;
                brush.Color = this._ForeColor;
                int n = this._Top + this._TitleHeight / 2 - 5;
                g.FillRectangle(dt.SolidBrush, this._Left + 4, n + 0, 2, 4);
                g.FillRectangle(dt.SolidBrush, this._Left + 6, n + 0, 2, 2);
                g.FillRectangle(dt.SolidBrush, this._Left + 8, n + 0, 2, 4);
                g.FillRectangle(dt.SolidBrush, this._Left + 3, n + 4, 8, 6);
                //g.DrawLine(dt.Pen, this._Left + 6, n + 5, this._Left + 6, n + 7);
                //g.DrawRectangle(dt.Pen, this._Left + 3, n + 0, 6, 3);
                //g.DrawRectangle(dt.Pen, this._Left + 2, n + 3, 8, 6);
                //g.DrawLine(dt.Pen, this._Left + 6, n + 5, this._Left + 6, n + 7);

            }
            if (this._LockLocation) {
                //dt.Pen.Color = this.ForeColor;
                brush.Color = this._ForeColor;
                int n = this._Top + this._TitleHeight / 2 - 5;
                g.FillRectangle(brush, this.Right - 9, n, 4, 4);
                g.FillRectangle(brush, this.Right - 11, n + 4, 8, 2);
                g.FillRectangle(brush, this.Right - 8, n + 6, 2, 4);
                //g.DrawLine(dt.Pen, this.Right - 10, n + 6, this.Right - 4, n + 6);
                //g.DrawLine(dt.Pen, this.Right - 10, n, this.Right - 4, n);
                //g.DrawLine(dt.Pen, this.Right - 11, n + 6, this.Right - 3, n + 6);
                //g.DrawLine(dt.Pen, this.Right - 7, n + 7, this.Right - 7, n + 9);
            }
            if (!string.IsNullOrEmpty(this._Title) && this._ForeColor.A != 0) {
                brush.Color = this._ForeColor;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.DrawString(this._Title, this._Font, brush, this.TitleRectangle, m_sf);
            }
        }
        /// <summary>
        /// 绘制Node主体部分 除去标题部分
        /// </summary>
        /// <param name="dt">绘制工具</param>
        protected virtual void OnDrawBody(DrawingTools dt) {
            SolidBrush brush = dt.SolidBrush;
            foreach (STNodeOption op in this._InputOptions) {
                if (op == STNodeOption.Empty) continue;
                this.OnDrawOptionDot(dt, op);
                this.OnDrawOptionText(dt, op);
            }
            foreach (STNodeOption op in this._OutputOptions) {
                if (op == STNodeOption.Empty) continue;
                this.OnDrawOptionDot(dt, op);
                this.OnDrawOptionText(dt, op);
            }
            if (this._Controls.Count != 0) {    //绘制子控件
                //将坐标原点与节点对齐
                //dt.Graphics.ResetTransform();
                dt.Graphics.TranslateTransform(this._Left, this._Top + this._TitleHeight);
                Point pt = Point.Empty;         //当前需要偏移的量 
                Point pt_last = Point.Empty;    //最后一个控件相对于节点的坐标
                foreach (STNodeControl v in this._Controls) {
                    if (!v.Visable) continue;
                    pt.X = v.Left - pt_last.X;
                    pt.Y = v.Top - pt_last.Y;
                    pt_last = v.Location;
                    dt.Graphics.TranslateTransform(pt.X, pt.Y); //将原点坐标移动至控件位置
                    dt.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                    v.OnPaint(dt);
                }
                //dt.Graphics.TranslateTransform(-pt_last.X, -pt_last.Y); 还原坐标
                dt.Graphics.TranslateTransform(-this._Left - pt_last.X, -this._Top - this._TitleHeight - pt_last.Y);
                //dt.Graphics.
            }
        }
        /// <summary>
        /// 绘制标记信息
        /// </summary>
        /// <param name="dt">绘制工具</param>
        protected internal virtual void OnDrawMark(DrawingTools dt) {
            if (string.IsNullOrEmpty(this._Mark)) return;
            Graphics g = dt.Graphics;
            SolidBrush brush = dt.SolidBrush;
            m_sf.LineAlignment = StringAlignment.Center;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            brush.Color = this._MarkColor;
            g.FillRectangle(brush, this._MarkRectangle);                                //填充背景色

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;       //确定文本绘制所需大小
            var sz = g.MeasureString(this.Mark, this.Font, this._MarkRectangle.Width);
            brush.Color = this._ForeColor;
            if (sz.Height > this._ItemHeight || sz.Width > this._MarkRectangle.Width) {    //如果超过绘图区 则绘制部分
                Rectangle rect = new Rectangle(this._MarkRectangle.Left + 2, this._MarkRectangle.Top + 2, this._MarkRectangle.Width - 20, 16);
                m_sf.Alignment = StringAlignment.Near;
                g.DrawString(this._MarkLines[0], this._Font, brush, rect, m_sf);
                m_sf.Alignment = StringAlignment.Far;
                rect.Width = this._MarkRectangle.Width - 5;
                g.DrawString("+", this._Font, brush, rect, m_sf);                       // + 表示超过绘图区
            } else {
                m_sf.Alignment = StringAlignment.Near;
                g.DrawString(this._MarkLines[0].Trim(), this._Font, brush, this._MarkRectangle, m_sf);
            }
        }
        /// <summary>
        /// 绘制选项连线的点
        /// </summary>
        /// <param name="dt">绘制工具</param>
        /// <param name="op">指定的选项</param>
        protected virtual void OnDrawOptionDot(DrawingTools dt, STNodeOption op) {
            Graphics g = dt.Graphics;
            Pen pen = dt.Pen;
            SolidBrush brush = dt.SolidBrush;
            var t = typeof(object);
            if (op.DotColor != Color.Transparent)           //设置颜色
                brush.Color = op.DotColor;
            else {
                if (op.DataType == t)
                    pen.Color = this.Owner.UnknownTypeColor;
                else
                    brush.Color = this.Owner.TypeColor.ContainsKey(op.DataType) ? this.Owner.TypeColor[op.DataType] : this.Owner.UnknownTypeColor;
            }
            if (op.IsSingle) {                              //单连接 圆形
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                if (op.DataType == t) {                     //未知类型绘制 否则填充
                    g.DrawEllipse(pen, op.DotRectangle.X, op.DotRectangle.Y, op.DotRectangle.Width - 1, op.DotRectangle.Height - 1);
                } else
                    g.FillEllipse(brush, op.DotRectangle);
            } else {                                        //多连接 矩形
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                if (op.DataType == t) {
                    g.DrawRectangle(pen, op.DotRectangle.X, op.DotRectangle.Y, op.DotRectangle.Width - 1, op.DotRectangle.Height - 1);
                } else
                    g.FillRectangle(brush, op.DotRectangle);
            }
        }
        /// <summary>
        /// 绘制选项的文本
        /// </summary>
        /// <param name="dt">绘制工具</param>
        /// <param name="op">指定的选项</param>
        protected virtual void OnDrawOptionText(DrawingTools dt, STNodeOption op) {
            Graphics g = dt.Graphics;
            SolidBrush brush = dt.SolidBrush;
            if (op.IsInput) {
                m_sf.Alignment = StringAlignment.Near;
            } else {
                m_sf.Alignment = StringAlignment.Far;
            }
            brush.Color = op.TextColor;
            g.DrawString(op.Text, this.Font, brush, op.TextRectangle, m_sf);
        }
        /// <summary>
        /// 当计算Option连线点位置时候发生
        /// </summary>
        /// <param name="op">需要计算的Option</param>
        /// <param name="pt">自动计算出的位置</param>
        /// <param name="nIndex">当前Option的索引</param>
        /// <returns>新的位置</returns>
        protected virtual Point OnSetOptionDotLocation(STNodeOption op, Point pt, int nIndex) {
            return pt;
        }
        /// <summary>
        /// 当计算Option文本区域时候发生
        /// </summary>
        /// <param name="op">需要计算的Option</param>
        /// <param name="rect">自动计算出的区域</param>
        /// <param name="nIndex">当前Option的索引</param>
        /// <returns>新的区域</returns>
        protected virtual Rectangle OnSetOptionTextRectangle(STNodeOption op, Rectangle rect, int nIndex) {
            return rect;
        }
        /// <summary>
        /// 获取当前STNode所需要的默认大小
        /// 返回的大小并不会限制绘制区域 任然可以在此区域之外绘制
        /// 但是并不会被STNodeEditor所接受 并触发对应事件
        /// </summary>
        /// <param name="g">绘图面板</param>
        /// <returns>计算出来的大小</returns>
        protected virtual Size GetDefaultNodeSize(Graphics g) {
            int nInputHeight = 0, nOutputHeight = 0;
            foreach (STNodeOption op in this._InputOptions) nInputHeight += this._ItemHeight;
            foreach (STNodeOption op in this._OutputOptions) nOutputHeight += this._ItemHeight;
            int nHeight = this._TitleHeight + (nInputHeight > nOutputHeight ? nInputHeight : nOutputHeight);

            SizeF szf_input = SizeF.Empty, szf_output = SizeF.Empty;
            foreach (STNodeOption v in this._InputOptions) {
                if (string.IsNullOrEmpty(v.Text)) continue;
                SizeF szf = g.MeasureString(v.Text, this._Font);
                if (szf.Width > szf_input.Width) szf_input = szf;
            }
            foreach (STNodeOption v in this._OutputOptions) {
                if (string.IsNullOrEmpty(v.Text)) continue;
                SizeF szf = g.MeasureString(v.Text, this._Font);
                if (szf.Width > szf_output.Width) szf_output = szf;
            }
            int nWidth = (int)(szf_input.Width + szf_output.Width + 25);
            if (!string.IsNullOrEmpty(this.Title)) szf_input = g.MeasureString(this.Title, this.Font);
            if (szf_input.Width + 30 > nWidth) nWidth = (int)szf_input.Width + 30;
            return new Size(nWidth, nHeight);
        }
        /// <summary>
        /// 计算当前Mark所需要的矩形区域
        /// 返回的大小并不会限制绘制区域 任然可以在此区域之外绘制
        /// 但是并不会被STNodeEditor所接受 并触发对应事件
        /// </summary>
        /// <param name="g">绘图面板</param>
        /// <returns>计算后的区域</returns>
        protected virtual Rectangle OnBuildMarkRectangle(Graphics g) {
            //if (string.IsNullOrEmpty(this._Mark)) return Rectangle.Empty;
            return new Rectangle(this._Left, this._Top - 30, this._Width, 20);
        }
        /// <summary>
        /// 当需要保存时候 此Node有哪些需要额外保存的数据
        /// 注意: 保存时并不会进行序列化 还原时候仅重新通过空参数构造器创建此Node
        ///       然后调用 OnLoadNode() 将保存的数据进行还原
        /// </summary>
        /// <param name="dic">需要保存的数据</param>
        protected virtual void OnSaveNode(Dictionary<string, byte[]> dic) { }
        /// <summary>
        /// 当还原该节点时候会将 OnSaveNode() 所返回的数据重新传入此函数
        /// </summary>
        /// <param name="dic">保存时候的数据</param>
        protected internal virtual void OnLoadNode(Dictionary<string, byte[]> dic) {
            if (dic.ContainsKey("AutoSize")) this._AutoSize = dic["AutoSize"][0] == 1;
            if (dic.ContainsKey("LockOption")) this._LockOption = dic["LockOption"][0] == 1;
            if (dic.ContainsKey("LockLocation")) this._LockLocation = dic["LockLocation"][0] == 1;
            if (dic.ContainsKey("Guid")) this._Guid = new Guid(dic["Guid"]);
            if (dic.ContainsKey("Left")) this._Left = BitConverter.ToInt32(dic["Left"], 0);
            if (dic.ContainsKey("Top")) this._Top = BitConverter.ToInt32(dic["Top"], 0);
            if (dic.ContainsKey("Width") && !this._AutoSize) this._Width = BitConverter.ToInt32(dic["Width"], 0);
            if (dic.ContainsKey("Height") && !this._AutoSize) this._Height = BitConverter.ToInt32(dic["Height"], 0);
            if (dic.ContainsKey("Mark")) this.Mark = Encoding.UTF8.GetString(dic["Mark"]);
            Type t = this.GetType();
            foreach (var p in t.GetProperties()) {
                var attrs = p.GetCustomAttributes(true);
                foreach (var a in attrs) {
                    if (!(a is STNodePropertyAttribute)) continue;
                    var attr = a as STNodePropertyAttribute;
                    object obj = Activator.CreateInstance(attr.DescriptorType);
                    if (!(obj is STNodePropertyDescriptor))
                        throw new InvalidOperationException("[STNodePropertyAttribute.Type]参数值必须为[STNodePropertyDescriptor]或者其子类的类型");
                    var desc = (STNodePropertyDescriptor)Activator.CreateInstance(attr.DescriptorType);
                    desc.Node = this;
                    desc.PropertyInfo = p;
                    try {
                        if (dic.ContainsKey(p.Name)) desc.SetValue(dic[p.Name]);
                    } catch (Exception ex) {
                        string strErr = "属性[" + this.Title + "." + p.Name + "]的值无法被还原 可通过重写[STNodePropertyAttribute.GetBytesFromValue(),STNodePropertyAttribute.GetValueFromBytes(byte[])]确保保存和加载时候的二进制数据正确";
                        Exception e = ex;
                        while (e != null) {
                            strErr += "\r\n----\r\n[" + e.GetType().Name + "] -> " + e.Message;
                            e = e.InnerException;
                        }
                        throw new InvalidOperationException(strErr, ex);
                    }
                }
            }
        }
        /// <summary>
        /// 当编辑器加载完成所有的节点时候发生
        /// </summary>
        protected internal virtual void OnEditorLoadCompleted() { }
        /// <summary>
        /// 设置Option的文本信息
        /// </summary>
        /// <param name="op">目标Option</param>
        /// <param name="strText">文本</param>
        /// <returns>是否成功</returns>
        protected bool SetOptionText(STNodeOption op, string strText) {
            if (op.Owner != this) return false;
            op.Text = strText;
            return true;
        }
        /// <summary>
        /// 设置Option文本信息颜色
        /// </summary>
        /// <param name="op">目标Option</param>
        /// <param name="clr">颜色</param>
        /// <returns>是否成功</returns>
        protected bool SetOptionTextColor(STNodeOption op, Color clr) {
            if (op.Owner != this) return false;
            op.TextColor = clr;
            return true;
        }
        /// <summary>
        /// 设置Option连线点颜色
        /// </summary>
        /// <param name="op">目标Option</param>
        /// <param name="clr">颜色</param>
        /// <returns>是否成功</returns>
        protected bool SetOptionDotColor(STNodeOption op, Color clr) {
            if (op.Owner != this) return false;
            op.DotColor = clr;
            return false;
        }

        //[event]===========================[event]==============================[event]============================[event]

        protected internal virtual void OnGotFocus(EventArgs e) { }

        protected internal virtual void OnLostFocus(EventArgs e) { }

        protected internal virtual void OnMouseEnter(EventArgs e) { }

        protected internal virtual void OnMouseDown(MouseEventArgs e) {
            Point pt = e.Location;
            pt.Y -= this._TitleHeight;
            for (int i = this._Controls.Count - 1; i >= 0; i--) {
                var c = this._Controls[i];
                if (c.DisplayRectangle.Contains(pt)) {
                    if (!c.Enabled) return;
                    if (!c.Visable) continue;
                    c.OnMouseDown(new MouseEventArgs(e.Button, e.Clicks, e.X - c.Left, pt.Y - c.Top, e.Delta));
                    m_ctrl_down = c;
                    if (m_ctrl_active != c) {
                        c.OnGotFocus(EventArgs.Empty);
                        if (m_ctrl_active != null) m_ctrl_active.OnLostFocus(EventArgs.Empty);
                        m_ctrl_active = c;
                    }
                    return;
                }
            }
            if (m_ctrl_active != null) m_ctrl_active.OnLostFocus(EventArgs.Empty);
            m_ctrl_active = null;
        }

        protected internal virtual void OnMouseMove(MouseEventArgs e) {
            Point pt = e.Location;
            pt.Y -= this._TitleHeight;
            if (m_ctrl_down != null) {
                if (m_ctrl_down.Enabled && m_ctrl_down.Visable)
                    m_ctrl_down.OnMouseMove(new MouseEventArgs(e.Button, e.Clicks, e.X - m_ctrl_down.Left, pt.Y - m_ctrl_down.Top, e.Delta));
                return;
            }
            for (int i = this._Controls.Count - 1; i >= 0; i--) {
                var c = this._Controls[i];
                if (c.DisplayRectangle.Contains(pt)) {
                    if (m_ctrl_hover != this._Controls[i]) {
                        c.OnMouseEnter(EventArgs.Empty);
                        if (m_ctrl_hover != null) m_ctrl_hover.OnMouseLeave(EventArgs.Empty);
                        m_ctrl_hover = c;
                    }
                    m_ctrl_hover.OnMouseMove(new MouseEventArgs(e.Button, e.Clicks, e.X - c.Left, pt.Y - c.Top, e.Delta));
                    return;
                }
            }
            if (m_ctrl_hover != null) m_ctrl_hover.OnMouseLeave(EventArgs.Empty);
            m_ctrl_hover = null;
        }

        protected internal virtual void OnMouseUp(MouseEventArgs e) {
            Point pt = e.Location;
            pt.Y -= this._TitleHeight;
            if (m_ctrl_down != null && m_ctrl_down.Enabled && m_ctrl_down.Visable) {
                m_ctrl_down.OnMouseUp(new MouseEventArgs(e.Button, e.Clicks, e.X - m_ctrl_down.Left, pt.Y - m_ctrl_down.Top, e.Delta));
            }
            //if (m_ctrl_active != null) {
            //    m_ctrl_active.OnMouseUp(new MouseEventArgs(e.Button, e.Clicks,
            //        e.X - m_ctrl_active.Left, pt.Y - m_ctrl_active.Top, e.Delta));
            //}
            m_ctrl_down = null;
        }

        protected internal virtual void OnMouseLeave(EventArgs e) {
            if (m_ctrl_hover != null && m_ctrl_hover.Enabled && m_ctrl_hover.Visable) m_ctrl_hover.OnMouseLeave(e);
            m_ctrl_hover = null;
        }

        protected internal virtual void OnMouseClick(MouseEventArgs e) {
            Point pt = e.Location;
            pt.Y -= this._TitleHeight;
            if (m_ctrl_active != null && m_ctrl_active.Enabled && m_ctrl_active.Visable)
                m_ctrl_active.OnMouseClick(new MouseEventArgs(e.Button, e.Clicks, e.X - m_ctrl_active.Left, pt.Y - m_ctrl_active.Top, e.Delta));
        }

        protected internal virtual void OnMouseWheel(MouseEventArgs e) {
            Point pt = e.Location;
            pt.Y -= this._TitleHeight;
            if (m_ctrl_hover != null && m_ctrl_active.Enabled && m_ctrl_hover.Visable) {
                m_ctrl_hover.OnMouseWheel(new MouseEventArgs(e.Button, e.Clicks, e.X - m_ctrl_hover.Left, pt.Y - m_ctrl_hover.Top, e.Delta));
                return;
            }
        }
        protected internal virtual void OnMouseHWheel(MouseEventArgs e) {
            if (m_ctrl_hover != null && m_ctrl_active.Enabled && m_ctrl_hover.Visable) {
                m_ctrl_hover.OnMouseHWheel(e);
                return;
            }
        }

        protected internal virtual void OnKeyDown(KeyEventArgs e) {
            if (m_ctrl_active != null && m_ctrl_active.Enabled && m_ctrl_active.Visable) m_ctrl_active.OnKeyDown(e);
        }
        protected internal virtual void OnKeyUp(KeyEventArgs e) {
            if (m_ctrl_active != null && m_ctrl_active.Enabled && m_ctrl_active.Visable) m_ctrl_active.OnKeyUp(e);
        }
        protected internal virtual void OnKeyPress(KeyPressEventArgs e) {
            if (m_ctrl_active != null && m_ctrl_active.Enabled && m_ctrl_active.Visable) m_ctrl_active.OnKeyPress(e);
        }

        protected virtual void OnMove(EventArgs e) { /*this.SetOptionLocation();*/ }
        protected virtual void OnResize(EventArgs e) { /*this.SetOptionLocation();*/ }


        /// <summary>
        /// 当所有者发生改变时候发生
        /// </summary>
        protected virtual void OnOwnerChanged() { }
        /// <summary>
        /// 当选中状态改变时候发生
        /// </summary>
        protected virtual void OnSelectedChanged() { }
        /// <summary>
        /// 当活动状态改变时候发生
        /// </summary>
        protected virtual void OnActiveChanged() { }

        #endregion protected
        /// <summary>
        /// 计算每个Option的位置
        /// </summary>
        protected virtual void SetOptionsLocation() {
            int nIndex = 0;
            Rectangle rect = new Rectangle(this.Left + 10, this._Top + this._TitleHeight, this._Width - 20, this._ItemHeight);
            foreach (STNodeOption op in this._InputOptions) {
                if (op != STNodeOption.Empty) {
                    Point pt = this.OnSetOptionDotLocation(op, new Point(this.Left - op.DotSize / 2, rect.Y + (rect.Height - op.DotSize) / 2), nIndex);
                    op.TextRectangle = this.OnSetOptionTextRectangle(op, rect, nIndex);
                    op.DotLeft = pt.X;
                    op.DotTop = pt.Y;
                }
                rect.Y += this._ItemHeight;
                nIndex++;
            }
            rect.Y = this._Top + this._TitleHeight;
            m_sf.Alignment = StringAlignment.Far;
            foreach (STNodeOption op in this._OutputOptions) {
                if (op != STNodeOption.Empty) {
                    Point pt = this.OnSetOptionDotLocation(op, new Point(this._Left + this._Width - op.DotSize / 2, rect.Y + (rect.Height - op.DotSize) / 2), nIndex);
                    op.TextRectangle = this.OnSetOptionTextRectangle(op, rect, nIndex);
                    op.DotLeft = pt.X;
                    op.DotTop = pt.Y;
                }
                rect.Y += this._ItemHeight;
                nIndex++;
            }
        }

        /// <summary>
        /// 重绘Node
        /// </summary>
        public void Invalidate() {
            if (this._Owner != null) {
                this._Owner.Invalidate(this._Owner.CanvasToControl(new Rectangle(this._Left - 5, this._Top - 5, this._Width + 10, this._Height + 10)));
            }
        }
        /// <summary>
        /// 重绘 Node 指定区域
        /// </summary>
        /// <param name="rect">Node 指定区域</param>
        public void Invalidate(Rectangle rect) {
            rect.X += this._Left;
            rect.Y += this._Top;
            if (this._Owner != null) {
                rect = this._Owner.CanvasToControl(rect);
                rect.Width += 1; rect.Height += 1;//坐标系统转换可能导致进度丢失 多加上一个像素
                this._Owner.Invalidate(rect);
            }
        }
        /// <summary>
        /// 获取此Node所包含的输入Option集合
        /// </summary>
        /// <returns>Option集合</returns>
        public STNodeOption[] GetInputOptions() {
            if (!this._LetGetOptions) return null;
            STNodeOption[] ops = new STNodeOption[this._InputOptions.Count];
            for (int i = 0; i < this._InputOptions.Count; i++) ops[i] = this._InputOptions[i];
            return ops;
        }
        /// <summary>
        /// 获取此Node所包含的输出Option集合
        /// </summary>
        /// <returns>Option集合</returns>
        public STNodeOption[] GetOutputOptions() {
            if (!this._LetGetOptions) return null;
            STNodeOption[] ops = new STNodeOption[this._OutputOptions.Count];
            for (int i = 0; i < this._OutputOptions.Count; i++) ops[i] = this._OutputOptions[i];
            return ops;
        }
        /// <summary>
        /// 设置Node的选中状态
        /// </summary>
        /// <param name="bSelected">是否选中</param>
        /// <param name="bRedraw">是否重绘</param>
        public void SetSelected(bool bSelected, bool bRedraw) {
            if (this._IsSelected == bSelected) return;
            this._IsSelected = bSelected;
            if (this._Owner != null) {
                if (bSelected)
                    this._Owner.AddSelectedNode(this);
                else
                    this._Owner.RemoveSelectedNode(this);
            }
            if (bRedraw) this.Invalidate();
            this.OnSelectedChanged();
            if (this._Owner != null) this._Owner.OnSelectedChanged(EventArgs.Empty);
        }
        public IAsyncResult BeginInvoke(Delegate method) { return this.BeginInvoke(method, null); }
        public IAsyncResult BeginInvoke(Delegate method, params object[] args) {
            if (this._Owner == null) return null;
            return this._Owner.BeginInvoke(method, args);
        }
        public object Invoke(Delegate method) { return this.Invoke(method, null); }
        public object Invoke(Delegate method, params object[] args) {
            if (this._Owner == null) return null;
            return this._Owner.Invoke(method, args);
        }
    }
}
