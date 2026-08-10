using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DrawingContext = System.Windows.Media.DrawingContext;
using WpfDpiScale = System.Windows.DpiScale;
using WpfPixelFormats = System.Windows.Media.PixelFormats;
using WpfVisualTreeHelper = System.Windows.Media.VisualTreeHelper;
using WpfDragEventArgs = System.Windows.DragEventArgs;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfMouseWheelEventArgs = System.Windows.Input.MouseWheelEventArgs;
using WpfPoint = System.Windows.Point;
using WpfRect = System.Windows.Rect;
using WpfTextCompositionEventArgs = System.Windows.Input.TextCompositionEventArgs;

namespace ST.Library.UI.NodeEditor;

public partial class STNodeEditor : System.Windows.Controls.Control, IDisposable
{
	protected enum CanvasAction
	{
		None,
		MoveNode,
		MoveCanvas,
		ConnectOption,
		SelectRectangle,
		DrawMarkDetails
	}

	protected struct MagnetInfo
	{
		public bool XMatched;

		public bool YMatched;

		public int X;

		public int Y;

		public int OffsetX;

		public int OffsetY;
	}

	protected static readonly Type m_type_node = typeof(STNode);

	private float _CanvasOffsetX;

	private float _CanvasOffsetY;

	private PointF _CanvasOffset;

	private Rectangle _CanvasValidBounds;

	private float _CanvasScale = 1f;

	private float _Curvature = 0.3f;

	private bool _ShowMagnet = true;

	private bool _ShowBorder = true;

	private bool _ShowNodeShadow = true;

	private int _NodeCornerRadius;

	private bool _ShowGrid = true;

	private bool _HighlightGridOrigin = true;

	private bool _ShowLocation = true;

	private bool _LimitCanvasToContentBounds = true;

	private bool _EnableBlankLeftDragCanvas = true;

	private bool _AutoSwitchCanvasDragBySelection;

	private bool _ShowCanvasDragLockButton = true;

	private STNodeCollection _Nodes;

	private STNode _ActiveNode;

	private STNode _HoverNode;

	private Color _GridColor = Color.Black;

	private Color _BorderColor = Color.Black;

	private Color _BorderHoverColor = Color.Gray;

	private Color _BorderSelectedColor = Color.Orange;

	private Color _BorderActiveColor = Color.OrangeRed;

	private Color _MarkForeColor = Color.White;

	private Color _MarkBackColor = Color.FromArgb(180, Color.Black);

	private Color _MagnetColor = Color.Lime;

	private Color _SelectedRectangleColor = Color.DodgerBlue;

	private Color _HighLineColor = Color.Cyan;

	private Color _LocationForeColor = Color.Red;

	private Color _LocationBackColor = Color.FromArgb(120, Color.Black);

	private Color _UnknownTypeColor = Color.Gray;

	private Dictionary<Type, Color> _TypeColor = new Dictionary<Type, Color>();

	private bool m_enableEdit;

	protected Point m_pt_in_control;

	protected PointF m_pt_in_canvas;

	protected Point m_pt_down_in_control;

	protected PointF m_pt_down_in_canvas;

	protected PointF m_pt_canvas_old;

	protected Point m_pt_dot_down;

	protected STNodeOption m_option_down;

	protected STNode m_node_down;

	protected bool m_mouse_in_control;

	private DrawingTools m_drawing_tools;

	private NodeFindInfo m_find;

	private MagnetInfo m_mi;

	private RectangleF m_rect_select;

	private readonly HashSet<STNode> m_rectangle_selection_baseline = new HashSet<STNode>();

	private Image m_img_border;

	private Image m_img_border_hover;

	private Image m_img_border_selected;

	private Image m_img_border_active;

	private float m_real_canvas_x;

	private float m_real_canvas_y;

	private Dictionary<STNode, Point> m_dic_pt_selected = new Dictionary<STNode, Point>();

	private List<int> m_lst_magnet_x = new List<int>();

	private List<int> m_lst_magnet_y = new List<int>();

	private List<int> m_lst_magnet_mx = new List<int>();

	private List<int> m_lst_magnet_my = new List<int>();

	private DateTime m_dt_vw = DateTime.Now;

	private DateTime m_dt_hw = DateTime.Now;

	private CanvasAction m_ca;

	private HashSet<STNode> m_hs_node_selected = new HashSet<STNode>();

	private bool m_is_process_mouse_event = true;

	private long m_suppress_context_menu_until;

	private bool m_is_buildpath;

	private Pen m_p_line = new Pen(Color.Cyan, 2f);

	private Pen m_p_line_hover = new Pen(Color.Cyan, 12f);

	private GraphicsPath m_gp_hover;

	private StringFormat m_sf = new StringFormat();

	private Rectangle m_rect_canvas_drag_lock;

	private Dictionary<GraphicsPath, ConnectionInfo> m_dic_gp_info = new Dictionary<GraphicsPath, ConnectionInfo>();

	private List<Point> m_lst_node_out = new List<Point>();

	private int m_time_alert;

	private int m_alpha_alert;

	private string m_str_alert;

	private Color m_forecolor_alert;

	private Color m_backcolor_alert;

	private DateTime m_dt_alert;

	private Rectangle m_rect_alert;

	private AlertLocation m_al;

	private Color _BackColor = Color.FromArgb(255, 34, 34, 34);

	private Color _ForeColor = Color.White;

	private Font _Font = new Font("Segoe UI", 9f);

	private Size _ClientSize = new Size(200, 200);

	private readonly Bitmap m_measurement_bitmap = new Bitmap(1, 1, PixelFormat.Format32bppPArgb);

	private readonly DispatcherTimer m_animation_timer;

	private Bitmap m_render_bitmap;

	private Graphics m_render_graphics;

	private WriteableBitmap m_render_target;

	private volatile bool m_disposed;

	private bool m_is_loaded;

	internal bool IsDisposed => m_disposed;

	[Browsable(false)]
	public float CanvasOffsetX => _CanvasOffsetX;

	[Browsable(false)]
	public float CanvasOffsetY => _CanvasOffsetY;

	[Browsable(false)]
	public PointF CanvasOffset
	{
		get
		{
			_CanvasOffset.X = _CanvasOffsetX;
			_CanvasOffset.Y = _CanvasOffsetY;
			return _CanvasOffset;
		}
	}

	[Browsable(false)]
	public Rectangle CanvasValidBounds => _CanvasValidBounds;

	[Browsable(false)]
	public float CanvasScale => _CanvasScale;

	[Browsable(false)]
	public Size ClientSize
	{
		get
		{
			int width = ActualWidth > 0 ? (int)Math.Ceiling(ActualWidth) : _ClientSize.Width;
			int height = ActualHeight > 0 ? (int)Math.Ceiling(ActualHeight) : _ClientSize.Height;
			return new Size(Math.Max(0, width), Math.Max(0, height));
		}
		set
		{
			_ClientSize = value;
			Invalidate();
		}
	}

	[Browsable(false)]
	public Rectangle ClientRectangle => new Rectangle(Point.Empty, ClientSize);

	[Description("获取或设置画布背景色")]
	public Color BackColor
	{
		get => _BackColor;
		set
		{
			_BackColor = value;
			Invalidate();
		}
	}

	[Description("获取或设置画布前景色")]
	public Color ForeColor
	{
		get => _ForeColor;
		set
		{
			_ForeColor = value;
			Invalidate();
		}
	}

	[Browsable(false)]
	public Font Font
	{
		get => _Font;
		set
		{
			if (value == null || ReferenceEquals(_Font, value))
			{
				return;
			}
			_Font.Dispose();
			_Font = value;
			Invalidate();
		}
	}

	[Browsable(false)]
	public float Curvature
	{
		get
		{
			return _Curvature;
		}
		set
		{
			if (value < 0f)
			{
				value = 0f;
			}
			if (value > 1f)
			{
				value = 1f;
			}
			_Curvature = value;
			if (m_dic_gp_info.Count != 0)
			{
				BuildLinePath();
			}
		}
	}

	[Description("获取或设置移动画布中 Node 时候 是否启用磁铁效果")]
	[DefaultValue(true)]
	public bool ShowMagnet
	{
		get
		{
			return _ShowMagnet;
		}
		set
		{
			_ShowMagnet = value;
		}
	}

	[Description("获取或设置 移动画布中是否显示 Node 边框")]
	[DefaultValue(true)]
	public bool ShowBorder
	{
		get
		{
			return _ShowBorder;
		}
		set
		{
			_ShowBorder = value;
			Invalidate();
		}
	}

	[Description("获取或设置是否绘制 Node 的阴影")]
	[DefaultValue(true)]
	public bool ShowNodeShadow
	{
		get
		{
			return _ShowNodeShadow;
		}
		set
		{
			if (_ShowNodeShadow == value)
			{
				return;
			}
			_ShowNodeShadow = value;
			Invalidate();
		}
	}

	[Description("获取或设置画布中 Node 的圆角半径")]
	[DefaultValue(0)]
	public int NodeCornerRadius
	{
		get
		{
			return _NodeCornerRadius;
		}
		set
		{
			if (value < 0)
			{
				value = 0;
			}
			if (_NodeCornerRadius == value)
			{
				return;
			}
			_NodeCornerRadius = value;
			Invalidate();
		}
	}

	[Description("获取或设置画布中是否绘制背景网格线条")]
	[DefaultValue(true)]
	public bool ShowGrid
	{
		get
		{
			return _ShowGrid;
		}
		set
		{
			_ShowGrid = value;
			Invalidate();
		}
	}

	[Description("获取或设置是否突出显示画布网格原点")]
	[DefaultValue(true)]
	public bool HighlightGridOrigin
	{
		get
		{
			return _HighlightGridOrigin;
		}
		set
		{
			if (_HighlightGridOrigin == value)
			{
				return;
			}
			_HighlightGridOrigin = value;
			Invalidate();
		}
	}

	[Description("获取或设置是否在画布边缘显示超出视角的 Node 位置信息")]
	[DefaultValue(true)]
	public bool ShowLocation
	{
		get
		{
			return _ShowLocation;
		}
		set
		{
			_ShowLocation = value;
			Invalidate();
		}
	}

	[Description("获取或设置移动和缩放画布时是否限制在 Node 内容范围内")]
	[DefaultValue(true)]
	public bool LimitCanvasToContentBounds
	{
		get
		{
			return _LimitCanvasToContentBounds;
		}
		set
		{
			_LimitCanvasToContentBounds = value;
		}
	}

	[Browsable(false)]
	public STNodeCollection Nodes => _Nodes;

	[Browsable(false)]
	public STNode ActiveNode => _ActiveNode;

	[Browsable(false)]
	public STNode HoverNode => _HoverNode;

	[Description("获取或设置绘制画布背景时 网格线条颜色")]
	[DefaultValue(typeof(Color), "Black")]
	public Color GridColor
	{
		get
		{
			return _GridColor;
		}
		set
		{
			_GridColor = value;
			Invalidate();
		}
	}

	[Description("获取或设置画布中 Node 边框颜色")]
	[DefaultValue(typeof(Color), "Black")]
	public Color BorderColor
	{
		get
		{
			return _BorderColor;
		}
		set
		{
			_BorderColor = value;
			if (m_img_border != null)
			{
				m_img_border.Dispose();
			}
			m_img_border = CreateBorderImage(value);
			Invalidate();
		}
	}

	[Description("获取或设置画布中悬停 Node 边框颜色")]
	[DefaultValue(typeof(Color), "Gray")]
	public Color BorderHoverColor
	{
		get
		{
			return _BorderHoverColor;
		}
		set
		{
			_BorderHoverColor = value;
			if (m_img_border_hover != null)
			{
				m_img_border_hover.Dispose();
			}
			m_img_border_hover = CreateBorderImage(value);
			Invalidate();
		}
	}

	[Description("获取或设置画布中选中 Node 边框颜色")]
	[DefaultValue(typeof(Color), "Orange")]
	public Color BorderSelectedColor
	{
		get
		{
			return _BorderSelectedColor;
		}
		set
		{
			_BorderSelectedColor = value;
			if (m_img_border_selected != null)
			{
				m_img_border_selected.Dispose();
			}
			m_img_border_selected = CreateBorderImage(value);
			Invalidate();
		}
	}

	[Description("获取或设置画布中活动 Node 边框颜色")]
	[DefaultValue(typeof(Color), "OrangeRed")]
	public Color BorderActiveColor
	{
		get
		{
			return _BorderActiveColor;
		}
		set
		{
			_BorderActiveColor = value;
			if (m_img_border_active != null)
			{
				m_img_border_active.Dispose();
			}
			m_img_border_active = CreateBorderImage(value);
			Invalidate();
		}
	}

	[Description("获取或设置画布绘制 Node 标记详情采用的前景色")]
	[DefaultValue(typeof(Color), "White")]
	public Color MarkForeColor
	{
		get
		{
			return _MarkForeColor;
		}
		set
		{
			_MarkForeColor = value;
			Invalidate();
		}
	}

	[Description("获取或设置画布绘制 Node 标记详情采用的背景色")]
	public Color MarkBackColor
	{
		get
		{
			return _MarkBackColor;
		}
		set
		{
			_MarkBackColor = value;
			Invalidate();
		}
	}

	[Description("获取或设置画布中移动 Node 时候 磁铁标记颜色")]
	[DefaultValue(typeof(Color), "Lime")]
	public Color MagnetColor
	{
		get
		{
			return _MagnetColor;
		}
		set
		{
			_MagnetColor = value;
		}
	}

	[Description("获取或设置画布中选择矩形区域的颜色")]
	[DefaultValue(typeof(Color), "DodgerBlue")]
	public Color SelectedRectangleColor
	{
		get
		{
			return _SelectedRectangleColor;
		}
		set
		{
			_SelectedRectangleColor = value;
		}
	}

	[Description("获取或设置画布中高亮连线的颜色")]
	[DefaultValue(typeof(Color), "Cyan")]
	public Color HighLineColor
	{
		get
		{
			return _HighLineColor;
		}
		set
		{
			_HighLineColor = value;
		}
	}

	[Description("获取或设置画布中边缘位置提示区域前景色")]
	[DefaultValue(typeof(Color), "Red")]
	public Color LocationForeColor
	{
		get
		{
			return _LocationForeColor;
		}
		set
		{
			_LocationForeColor = value;
			Invalidate();
		}
	}

	[Description("获取或设置画布中边缘位置提示区域背景色")]
	public Color LocationBackColor
	{
		get
		{
			return _LocationBackColor;
		}
		set
		{
			_LocationBackColor = value;
			Invalidate();
		}
	}

	[Description("获取或设置画布中当 Node 中 Option 数据类型无法确定时应当使用的颜色")]
	[DefaultValue(typeof(Color), "Gray")]
	public Color UnknownTypeColor
	{
		get
		{
			return _UnknownTypeColor;
		}
		set
		{
			_UnknownTypeColor = value;
			Invalidate();
		}
	}

	[Browsable(false)]
	public Dictionary<Type, Color> TypeColor => _TypeColor;

	[Browsable(false)]
	public bool EnableEdit
	{
		get
		{
			return m_enableEdit;
		}
		set
		{
			m_enableEdit = value;
		}
	}

	public bool EnableBlankLeftDragCanvas
	{
		get
		{
			return _EnableBlankLeftDragCanvas;
		}
		set
		{
			if (_EnableBlankLeftDragCanvas == value)
			{
				return;
			}
			_EnableBlankLeftDragCanvas = value;
			Invalidate();
			EnableBlankLeftDragCanvasChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	[Description("根据节点选中状态自动切换空白区域左键拖动画布：无选中节点时启用，有选中节点时禁用")]
	[DefaultValue(false)]
	public bool AutoSwitchCanvasDragBySelection
	{
		get
		{
			return _AutoSwitchCanvasDragBySelection;
		}
		set
		{
			if (_AutoSwitchCanvasDragBySelection == value)
			{
				return;
			}
			_AutoSwitchCanvasDragBySelection = value;
			UpdateCanvasDragModeFromSelection();
		}
	}

	[DefaultValue(true)]
	public bool ShowCanvasDragLockButton
	{
		get
		{
			return _ShowCanvasDragLockButton;
		}
		set
		{
			if (_ShowCanvasDragLockButton == value)
			{
				return;
			}
			_ShowCanvasDragLockButton = value;
			if (!value)
			{
				m_rect_canvas_drag_lock = Rectangle.Empty;
			}
			Invalidate();
		}
	}

	public event EventHandler EnableBlankLeftDragCanvasChanged;

	[Description("活动的节点发生变化时候发生")]
	public event EventHandler ActiveChanged;

	[Description("选择的节点发生变化时候发生")]
	public event EventHandler SelectedChanged;

	[Description("悬停的节点发生变化时候发生")]
	public event EventHandler HoverChanged;

	[Description("当节点被添加时候发生")]
	public event STNodeEditorEventHandler NodeAdded;

	[Description("当节点被移除时候发生")]
	public event STNodeEditorEventHandler NodeRemoved;

	[Description("移动画布原点时候发生")]
	public event EventHandler CanvasMoved;

	[Description("缩放画布时候发生")]
	public event EventHandler CanvasScaled;

	[Description("连接节点选项时候发生")]
	public event STNodeEditorOptionEventHandler OptionConnected;

	[Description("正在连接节点选项时候发生")]
	public event STNodeEditorOptionEventHandler OptionConnecting;

	[Description("断开节点选项时候发生")]
	public event STNodeEditorOptionEventHandler OptionDisConnected;

	[Description("正在断开节点选项时候发生")]
	public event STNodeEditorOptionEventHandler OptionDisConnecting;

	public STNodeEditor()
	{
		Focusable = true;
		ClipToBounds = true;
		SnapsToDevicePixels = true;
		UseLayoutRounding = true;
		AllowDrop = true;
		MinWidth = 100;
		MinHeight = 100;
		_Nodes = new STNodeCollection(this);
		m_enableEdit = true;
		m_real_canvas_x = (_CanvasOffsetX = 10f);
		m_real_canvas_y = (_CanvasOffsetY = 10f);
		STNodeTypeRegistry.Initialize();
		InitializeEditing();
		InitializeDrawingResources();
		m_animation_timer = new DispatcherTimer(DispatcherPriority.Render)
		{
			Interval = TimeSpan.FromMilliseconds(30)
		};
		m_animation_timer.Tick += AnimationTimer_Tick;
		Loaded += (_, _) =>
		{
			m_is_loaded = true;
			UpdateAnimationTimerState();
		};
		Unloaded += (_, _) =>
		{
			m_is_loaded = false;
			m_animation_timer.Stop();
		};
	}

	protected internal virtual void OnSelectedChanged(EventArgs e)
	{
		UpdateCanvasDragModeFromSelection();
		if (this.SelectedChanged != null)
		{
			this.SelectedChanged(this, e);
		}
	}

	private void UpdateCanvasDragModeFromSelection()
	{
		if (!_AutoSwitchCanvasDragBySelection)
		{
			return;
		}
		bool hasSelection;
		lock (m_hs_node_selected)
		{
			hasSelection = m_hs_node_selected.Count > 0;
		}
		EnableBlankLeftDragCanvas = !hasSelection;
	}

	protected virtual void OnActiveChanged(EventArgs e)
	{
		if (this.ActiveChanged != null)
		{
			this.ActiveChanged(this, e);
		}
	}

	protected virtual void OnHoverChanged(EventArgs e)
	{
		if (this.HoverChanged != null)
		{
			this.HoverChanged(this, e);
		}
	}

	protected internal virtual void OnNodeAdded(STNodeEditorEventArgs e)
	{
		TrackNode(e.Node);
		if (this.NodeAdded != null)
		{
			this.NodeAdded(this, e);
		}
	}

	protected internal virtual void OnNodeRemoved(STNodeEditorEventArgs e)
	{
		UntrackNode(e.Node);
		if (this.NodeRemoved != null)
		{
			this.NodeRemoved(this, e);
		}
	}

	protected virtual void OnCanvasMoved(EventArgs e)
	{
		if (this.CanvasMoved != null)
		{
			this.CanvasMoved(this, e);
		}
	}

	protected virtual void OnCanvasScaled(EventArgs e)
	{
		if (this.CanvasScaled != null)
		{
			this.CanvasScaled(this, e);
		}
	}

	protected internal virtual void OnOptionConnected(STNodeEditorOptionEventArgs e)
	{
		CompleteConnectionChange(e.CurrentOption, e.TargetOption, e.Status == ConnectionStatus.Connected ? true : (bool?)null);
		if (this.OptionConnected != null)
		{
			this.OptionConnected(this, e);
		}
	}

	protected internal virtual void OnOptionDisConnected(STNodeEditorOptionEventArgs e)
	{
		CompleteConnectionChange(e.CurrentOption, e.TargetOption, e.Status == ConnectionStatus.DisConnected ? false : (bool?)null);
		if (this.OptionDisConnected != null)
		{
			this.OptionDisConnected(this, e);
		}
	}

	protected internal virtual void OnOptionConnecting(STNodeEditorOptionEventArgs e)
	{
		PrepareConnectionChange(e.CurrentOption, e.TargetOption);
		if (IsReplayingHistory)
		{
			return;
		}
		if (this.OptionConnecting != null)
		{
			this.OptionConnecting(this, e);
		}
	}

	protected internal virtual void OnOptionDisConnecting(STNodeEditorOptionEventArgs e)
	{
		PrepareConnectionChange(e.CurrentOption, e.TargetOption);
		if (IsReplayingHistory)
		{
			return;
		}
		if (this.OptionDisConnecting != null)
		{
			this.OptionDisConnecting(this, e);
		}
	}

	private void InitializeDrawingResources()
	{
		m_drawing_tools = new DrawingTools
		{
			Pen = new Pen(Color.Black, 1f),
			SolidBrush = new SolidBrush(Color.Black)
		};
		m_img_border = CreateBorderImage(_BorderColor);
		m_img_border_active = CreateBorderImage(_BorderActiveColor);
		m_img_border_hover = CreateBorderImage(_BorderHoverColor);
		m_img_border_selected = CreateBorderImage(_BorderSelectedColor);
		m_sf?.Dispose();
		m_sf = new StringFormat
		{
			Alignment = StringAlignment.Near,
			FormatFlags = StringFormatFlags.NoWrap
		};
		m_sf.SetTabStops(0f, new float[1] { 40f });
	}

	private void AnimationTimer_Tick(object sender, EventArgs e)
	{
		if (m_disposed)
		{
			m_animation_timer.Stop();
			return;
		}

		bool redraw = false;
		float nextX = MoveTowards(_CanvasOffsetX, m_real_canvas_x);
		float nextY = MoveTowards(_CanvasOffsetY, m_real_canvas_y);
		if (nextX != _CanvasOffsetX || nextY != _CanvasOffsetY)
		{
			_CanvasOffsetX = nextX;
			_CanvasOffsetY = nextY;
			m_pt_canvas_old.X = nextX;
			m_pt_canvas_old.Y = nextY;
			redraw = true;
		}

		double remaining = m_time_alert - DateTime.UtcNow.Subtract(m_dt_alert).TotalMilliseconds;
		int alpha = remaining >= 0 ? 255 : remaining <= -1000 ? 0 : (int)(255d + remaining / 1000d * 255d);
		if (alpha != m_alpha_alert)
		{
			m_alpha_alert = alpha;
			redraw = true;
		}
		if (redraw)
		{
			Invalidate();
		}
		UpdateAnimationTimerState();
	}

	private bool HasPendingAnimation()
	{
		return _CanvasOffsetX != m_real_canvas_x
			|| _CanvasOffsetY != m_real_canvas_y
			|| m_alpha_alert > 0;
	}

	private void UpdateAnimationTimerState()
	{
		if (!m_disposed && m_is_loaded && HasPendingAnimation())
		{
			if (!m_animation_timer.IsEnabled)
				m_animation_timer.Start();
		}
		else if (m_animation_timer.IsEnabled)
		{
			m_animation_timer.Stop();
		}
	}

	private static float MoveTowards(float current, float target)
	{
		float delta = target - current;
		float distance = Math.Abs(delta);
		if (distance < 1f)
		{
			return target;
		}
		float step = distance <= 4f ? 1f : distance <= 12f ? 2f : distance <= 30f ? 3f : distance / 10f;
		return current + (delta > 0f ? step : -step);
	}

	protected override void OnRender(DrawingContext drawingContext)
	{
		base.OnRender(drawingContext);
		if (m_disposed)
		{
			return;
		}

		Size clientSize = ClientSize;
		if (clientSize.Width <= 0 || clientSize.Height <= 0)
		{
			return;
		}

		double width = ActualWidth > 0 ? ActualWidth : clientSize.Width;
		double height = ActualHeight > 0 ? ActualHeight : clientSize.Height;
		WpfDpiScale dpi = WpfVisualTreeHelper.GetDpi(this);
		int pixelWidth = Math.Max(1, (int)Math.Ceiling(width * dpi.DpiScaleX));
		int pixelHeight = Math.Max(1, (int)Math.Ceiling(height * dpi.DpiScaleY));

		EnsureRenderTarget(pixelWidth, pixelHeight, dpi.PixelsPerInchX, dpi.PixelsPerInchY);
		RenderToGraphics(
			m_render_graphics,
			clientSize.Width,
			clientSize.Height,
			(float)dpi.DpiScaleX,
			(float)dpi.DpiScaleY);
		BitmapData bitmapData = m_render_bitmap.LockBits(
			new Rectangle(0, 0, pixelWidth, pixelHeight),
			ImageLockMode.ReadOnly,
			PixelFormat.Format32bppPArgb);
		try
		{
			m_render_target.WritePixels(
				new System.Windows.Int32Rect(0, 0, pixelWidth, pixelHeight),
				bitmapData.Scan0,
				Math.Abs(bitmapData.Stride) * pixelHeight,
				bitmapData.Stride);
		}
		finally
		{
			m_render_bitmap.UnlockBits(bitmapData);
		}

		drawingContext.DrawImage(m_render_target, new WpfRect(0, 0, width, height));
	}

	private void EnsureRenderTarget(int pixelWidth, int pixelHeight, double dpiX, double dpiY)
	{
		if (m_render_bitmap != null &&
			m_render_bitmap.Width == pixelWidth &&
			m_render_bitmap.Height == pixelHeight &&
			Math.Abs(m_render_target.DpiX - dpiX) < 0.01 &&
			Math.Abs(m_render_target.DpiY - dpiY) < 0.01)
		{
			return;
		}
		m_render_graphics?.Dispose();
		m_render_bitmap?.Dispose();
		m_render_bitmap = new Bitmap(pixelWidth, pixelHeight, PixelFormat.Format32bppPArgb);
		m_render_graphics = Graphics.FromImage(m_render_bitmap);
		m_render_target = new WriteableBitmap(pixelWidth, pixelHeight, dpiX, dpiY, WpfPixelFormats.Pbgra32, null);
	}

	private void RenderToGraphics(Graphics graphics, int width, int height, float dpiScaleX, float dpiScaleY)
	{
		ResetRenderTransform(graphics, dpiScaleX, dpiScaleY);
		graphics.Clear(BackColor);
		graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
		graphics.SmoothingMode = SmoothingMode.HighQuality;
		m_drawing_tools.Graphics = graphics;
		if (_ShowGrid)
		{
			OnDrawGrid(m_drawing_tools, width, height);
		}
		graphics.TranslateTransform(_CanvasOffsetX, _CanvasOffsetY);
		graphics.ScaleTransform(_CanvasScale, _CanvasScale);
		OnDrawConnectedLine(m_drawing_tools);
		OnDrawNode(m_drawing_tools, ControlToCanvas(new Rectangle(0, 0, width, height)));
		if (m_ca == CanvasAction.ConnectOption && m_option_down != null)
		{
			m_drawing_tools.Pen.Color = _HighLineColor;
			if (m_option_down.IsInput)
			{
				DrawBezier(graphics, m_drawing_tools.Pen, m_pt_in_canvas, m_pt_dot_down, _Curvature);
			}
			else
			{
				DrawBezier(graphics, m_drawing_tools.Pen, m_pt_dot_down, m_pt_in_canvas, _Curvature);
			}
		}
		ResetRenderTransform(graphics, dpiScaleX, dpiScaleY);
		switch (m_ca)
		{
		case CanvasAction.MoveNode:
			if (_ShowMagnet && _ActiveNode != null)
			{
				OnDrawMagnet(m_drawing_tools, m_mi);
			}
			break;
		case CanvasAction.SelectRectangle:
			OnDrawSelectedRectangle(m_drawing_tools, CanvasToControl(m_rect_select));
			break;
		case CanvasAction.DrawMarkDetails:
			if (!string.IsNullOrEmpty(m_find.Mark))
			{
				OnDrawMark(m_drawing_tools);
			}
			break;
		}
		if (_ShowLocation)
		{
			OnDrawNodeOutLocation(m_drawing_tools, new Size(width, height), m_lst_node_out);
		}
		OnDrawAlert(graphics);
		if (_ShowCanvasDragLockButton)
		{
			OnDrawCanvasDragLockButton(m_drawing_tools);
		}
	}

	private static void ResetRenderTransform(Graphics graphics, float dpiScaleX, float dpiScaleY)
	{
		graphics.ResetTransform();
		graphics.ScaleTransform(dpiScaleX, dpiScaleY);
	}

	protected override void OnMouseDown(WpfMouseButtonEventArgs e)
	{
		base.OnMouseDown(e);
		STNodeMouseEventArgs nodeEvent = CreateMouseEventArgs(e);
		Focus();
		CaptureMouse();
		if (_ShowCanvasDragLockButton
			&& nodeEvent.Button == STMouseButtons.Left
			&& m_rect_canvas_drag_lock.Contains(nodeEvent.Location))
		{
			EnableBlankLeftDragCanvas = !EnableBlankLeftDragCanvas;
			m_ca = CanvasAction.None;
			e.Handled = true;
			return;
		}
		m_ca = CanvasAction.None;
		if (!ShouldActivateNodeFromMouse(nodeEvent.Button))
		{
			SetActiveNode(null);
		}
		m_mi.XMatched = (m_mi.YMatched = false);
		m_pt_down_in_control = nodeEvent.Location;
		m_pt_down_in_canvas.X = ((float)nodeEvent.X - _CanvasOffsetX) / _CanvasScale;
		m_pt_down_in_canvas.Y = ((float)nodeEvent.Y - _CanvasOffsetY) / _CanvasScale;
		m_pt_canvas_old.X = _CanvasOffsetX;
		m_pt_canvas_old.Y = _CanvasOffsetY;
		if (m_gp_hover != null && nodeEvent.Button == STMouseButtons.Right)
		{
			NodeFindInfo preCheck = FindNodeFromPoint(m_pt_down_in_canvas);
			if (preCheck.Node != null)
			{
				m_gp_hover = null;
			}
			else
			{
				if (m_enableEdit)
				{
					bool hadHoveredConnection = m_gp_hover != null && m_dic_gp_info.ContainsKey(m_gp_hover);
					ConnectionStatus disconnectStatus;
					using (BeginEditTransaction("断开连接"))
					{
						disconnectStatus = DisConnectionHover();
					}
					if (hadHoveredConnection && disconnectStatus == ConnectionStatus.DisConnected)
					{
						m_suppress_context_menu_until = Stopwatch.GetTimestamp() + Stopwatch.Frequency;
					}
					m_is_process_mouse_event = false;
				}
				return;
			}
		}
		NodeFindInfo nodeFindInfo = FindNodeFromPoint(m_pt_down_in_canvas);
		if (nodeFindInfo.Node != null && !ShouldActivateNodeFromMouse(nodeEvent.Button))
		{
			return;
		}
		if (!string.IsNullOrEmpty(nodeFindInfo.Mark))
		{
			m_ca = CanvasAction.DrawMarkDetails;
			Invalidate();
		}
		else if (nodeFindInfo.NodeOption != null)
		{
			if (m_enableEdit)
			{
				StartConnect(nodeFindInfo.NodeOption);
			}
		}
		else if (nodeFindInfo.Node != null)
		{
			if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
			{
				if (nodeFindInfo.Node.IsSelected)
				{
					if (nodeFindInfo.Node == _ActiveNode)
					{
						SetActiveNode(null);
					}
					nodeFindInfo.Node.SetSelected(bSelected: false, bRedraw: true);
				}
				else
				{
					nodeFindInfo.Node.SetSelected(bSelected: true, bRedraw: true);
				}
				m_node_down = null;
				m_is_process_mouse_event = false;
				e.Handled = true;
				return;
			}
			nodeFindInfo.Node.OnMouseDown(nodeEvent.WithLocation((int)m_pt_down_in_canvas.X - nodeFindInfo.Node.Left, (int)m_pt_down_in_canvas.Y - nodeFindInfo.Node.Top));
			if (!nodeFindInfo.Node.IsSelected)
			{
				STNode[] array = m_hs_node_selected.ToArray();
				foreach (STNode sTNode in array)
				{
					sTNode.SetSelected(bSelected: false, bRedraw: false);
				}
			}
			nodeFindInfo.Node.SetSelected(bSelected: true, bRedraw: false);
			SetActiveNode(nodeFindInfo.Node);
			if (PointInRectangle(nodeFindInfo.Node.TitleRectangle, m_pt_down_in_canvas.X, m_pt_down_in_canvas.Y))
			{
				m_dic_pt_selected.Clear();
				lock (m_hs_node_selected)
				{
					foreach (STNode item in m_hs_node_selected)
					{
						m_dic_pt_selected.Add(item, item.Location);
					}
				}
				m_ca = CanvasAction.MoveNode;
				BeginPointerEdit("移动节点");
				if (_ShowMagnet && _ActiveNode != null)
				{
					BuildMagnetLocation();
				}
			}
			else
			{
				m_node_down = nodeFindInfo.Node;
			}
		}
		else
		{
			bool enableBlankLeftDragCanvasAtMouseDown = EnableBlankLeftDragCanvas;
			ModifierKeys modifiers = Keyboard.Modifiers;
			SetActiveNode(null);
			bool panCanvas = ShouldPanBlankCanvas(nodeEvent.Button, enableBlankLeftDragCanvasAtMouseDown, modifiers);
			m_rectangle_selection_baseline.Clear();
			if (!panCanvas)
			{
				bool appendSelection = (modifiers & ModifierKeys.Control) == ModifierKeys.Control;
				if (appendSelection)
				{
					foreach (STNode selectedNode in m_hs_node_selected)
					{
						m_rectangle_selection_baseline.Add(selectedNode);
					}
				}
				else
				{
					STNode[] array2 = m_hs_node_selected.ToArray();
					foreach (STNode sTNode2 in array2)
					{
						sTNode2.SetSelected(bSelected: false, bRedraw: false);
					}
				}
			}
			m_ca = panCanvas ? CanvasAction.MoveCanvas : CanvasAction.SelectRectangle;
			ref RectangleF rect_select = ref m_rect_select;
			float num = (m_rect_select.Height = 0f);
			rect_select.Width = num;
			m_node_down = null;
		}
		e.Handled = nodeEvent.Button != STMouseButtons.Right;
	}

	protected internal static bool ShouldActivateNodeFromMouse(STMouseButtons button)
	{
		return button == STMouseButtons.Left;
	}

	protected internal static bool ShouldPanBlankCanvas(STMouseButtons button, bool enableBlankLeftDragCanvasAtMouseDown, ModifierKeys modifiers)
	{
		return button == STMouseButtons.Middle
			|| button == STMouseButtons.Left
			&& enableBlankLeftDragCanvasAtMouseDown
			&& (modifiers & ModifierKeys.Control) != ModifierKeys.Control;
	}

	protected internal static bool ShouldSelectNodeFromRectangle(bool intersectsSelectionRectangle, bool wasSelectedBeforeDrag)
	{
		return intersectsSelectionRectangle || wasSelectedBeforeDrag;
	}

	protected override void OnMouseMove(WpfMouseEventArgs e)
	{
		base.OnMouseMove(e);
		STNodeMouseEventArgs nodeEvent = CreateMouseEventArgs(e);
		m_pt_in_control = nodeEvent.Location;
		m_pt_in_canvas.X = ((float)nodeEvent.X - _CanvasOffsetX) / _CanvasScale;
		m_pt_in_canvas.Y = ((float)nodeEvent.Y - _CanvasOffsetY) / _CanvasScale;
		if (m_node_down != null)
		{
			m_node_down.OnMouseMove(nodeEvent.WithLocation((int)m_pt_in_canvas.X - m_node_down.Left, (int)m_pt_in_canvas.Y - m_node_down.Top));
			return;
		}
		if (nodeEvent.Button == STMouseButtons.Middle)
		{
			_CanvasOffsetX = (m_real_canvas_x = m_pt_canvas_old.X + (float)(nodeEvent.X - m_pt_down_in_control.X));
			_CanvasOffsetY = (m_real_canvas_y = m_pt_canvas_old.Y + (float)(nodeEvent.Y - m_pt_down_in_control.Y));
			Invalidate();
			return;
		}
		if (nodeEvent.Button == STMouseButtons.Left)
		{
			m_gp_hover = null;
			switch (m_ca)
			{
			case CanvasAction.MoveNode:
				if (m_enableEdit)
				{
					MoveNode(nodeEvent.Location);
				}
				return;
			case CanvasAction.MoveCanvas:
				_CanvasOffsetX = (m_real_canvas_x = m_pt_canvas_old.X + (float)(nodeEvent.X - m_pt_down_in_control.X));
				_CanvasOffsetY = (m_real_canvas_y = m_pt_canvas_old.Y + (float)(nodeEvent.Y - m_pt_down_in_control.Y));
				Invalidate();
				return;
			case CanvasAction.ConnectOption:
				Invalidate();
				return;
			case CanvasAction.SelectRectangle:
				m_rect_select.X = ((m_pt_down_in_canvas.X < m_pt_in_canvas.X) ? m_pt_down_in_canvas.X : m_pt_in_canvas.X);
				m_rect_select.Y = ((m_pt_down_in_canvas.Y < m_pt_in_canvas.Y) ? m_pt_down_in_canvas.Y : m_pt_in_canvas.Y);
				m_rect_select.Width = Math.Abs(m_pt_in_canvas.X - m_pt_down_in_canvas.X);
				m_rect_select.Height = Math.Abs(m_pt_in_canvas.Y - m_pt_down_in_canvas.Y);
				foreach (STNode node in _Nodes)
				{
					node.SetSelected(
						ShouldSelectNodeFromRectangle(
							m_rect_select.IntersectsWith(node.Rectangle),
							m_rectangle_selection_baseline.Contains(node)),
						bRedraw: false);
				}
				Invalidate();
				return;
			}
		}
		NodeFindInfo nodeFindInfo = FindNodeFromPoint(m_pt_in_canvas);
		bool flag = false;
		if (_HoverNode != nodeFindInfo.Node)
		{
			if (nodeFindInfo.Node != null)
			{
				nodeFindInfo.Node.OnMouseEnter(EventArgs.Empty);
			}
			if (_HoverNode != null)
			{
				_HoverNode.OnMouseLeave(nodeEvent.WithLocation((int)m_pt_in_canvas.X - _HoverNode.Left, (int)m_pt_in_canvas.Y - _HoverNode.Top));
			}
			_HoverNode = nodeFindInfo.Node;
			OnHoverChanged(EventArgs.Empty);
			flag = true;
		}
		if (_HoverNode != null)
		{
			_HoverNode.OnMouseMove(nodeEvent.WithLocation((int)m_pt_in_canvas.X - _HoverNode.Left, (int)m_pt_in_canvas.Y - _HoverNode.Top));
			m_gp_hover = null;
		}
		else
		{
			GraphicsPath graphicsPath = null;
			foreach (KeyValuePair<GraphicsPath, ConnectionInfo> item in m_dic_gp_info)
			{
				if (item.Key.IsOutlineVisible(m_pt_in_canvas, m_p_line_hover))
				{
					graphicsPath = item.Key;
					break;
				}
			}
			if (m_gp_hover != graphicsPath)
			{
				m_gp_hover = graphicsPath;
				flag = true;
			}
		}
		if (flag)
		{
			Invalidate();
		}
	}

	protected override void OnMouseUp(WpfMouseButtonEventArgs e)
	{
		base.OnMouseUp(e);
		STNodeMouseEventArgs nodeEvent = CreateMouseEventArgs(e);
		m_pt_in_control = nodeEvent.Location;
		m_pt_in_canvas.X = ((float)nodeEvent.X - _CanvasOffsetX) / _CanvasScale;
		m_pt_in_canvas.Y = ((float)nodeEvent.Y - _CanvasOffsetY) / _CanvasScale;
		int dotPadding = (m_ca == CanvasAction.ConnectOption) ? 14 : 6;
		NodeFindInfo nodeFindInfo = FindNodeFromPoint(m_pt_in_canvas, dotPadding);
		switch (m_ca)
		{
		case CanvasAction.MoveNode:
			break;
		case CanvasAction.ConnectOption:
			if (!(nodeEvent.Location == m_pt_down_in_control) && nodeFindInfo.NodeOption != null)
			{
				if (m_option_down.IsInput)
				{
					nodeFindInfo.NodeOption.ConnectOption(m_option_down);
				}
				else
				{
					m_option_down.ConnectOption(nodeFindInfo.NodeOption);
				}
			}
			break;
		}
		EndPointerEdit();
		if (m_is_process_mouse_event && _ActiveNode != null)
		{
			STNodeMouseEventArgs e2 = nodeEvent.WithLocation((int)m_pt_in_canvas.X - _ActiveNode.Left, (int)m_pt_in_canvas.Y - _ActiveNode.Top);
			_ActiveNode.OnMouseUp(e2);
			m_node_down = null;
		}
		if (Math.Abs(nodeEvent.X - m_pt_down_in_control.X) <= 2 && Math.Abs(nodeEvent.Y - m_pt_down_in_control.Y) <= 2)
		{
			ProcessMouseClick(nodeEvent);
		}
		m_is_process_mouse_event = true;
		m_ca = CanvasAction.None;
		m_rectangle_selection_baseline.Clear();
		ReleaseMouseCapture();
		Invalidate();
	}

	protected override void OnContextMenuOpening(System.Windows.Controls.ContextMenuEventArgs e)
	{
		long suppressUntil = m_suppress_context_menu_until;
		m_suppress_context_menu_until = 0;
		if (suppressUntil >= Stopwatch.GetTimestamp())
		{
			e.Handled = true;
			return;
		}
		base.OnContextMenuOpening(e);
	}

	protected override void OnLostMouseCapture(WpfMouseEventArgs e)
	{
		EndPointerEdit();
		_ActiveNode?.CancelMouseInteraction();
		m_node_down?.CancelMouseInteraction();
		m_rectangle_selection_baseline.Clear();
		m_node_down = null;
		m_option_down = null;
		m_rect_select = RectangleF.Empty;
		m_ca = CanvasAction.None;
		m_is_process_mouse_event = true;
		Invalidate();
		base.OnLostMouseCapture(e);
	}

	protected override void OnMouseEnter(WpfMouseEventArgs e)
	{
		base.OnMouseEnter(e);
		m_mouse_in_control = true;
	}

	protected override void OnMouseLeave(WpfMouseEventArgs e)
	{
		base.OnMouseLeave(e);
		m_mouse_in_control = false;
		if (_HoverNode != null)
		{
			_HoverNode.OnMouseLeave(e);
		}
		_HoverNode = null;
		Invalidate();
	}

	protected override void OnMouseWheel(WpfMouseWheelEventArgs e)
	{
		base.OnMouseWheel(e);
		STNodeMouseEventArgs nodeEvent = CreateMouseEventArgs(e);
		m_pt_in_control = nodeEvent.Location;
		m_pt_in_canvas.X = ((float)nodeEvent.X - _CanvasOffsetX) / _CanvasScale;
		m_pt_in_canvas.Y = ((float)nodeEvent.Y - _CanvasOffsetY) / _CanvasScale;
		float scale = _CanvasScale + (nodeEvent.Delta < 0 ? -0.05f : 0.05f);
		ScaleCanvas(scale, nodeEvent.X, nodeEvent.Y);
		e.Handled = true;
	}

	protected virtual void OnMouseHWheel(STNodeMouseEventArgs e)
	{
		if (m_mouse_in_control && _HoverNode != null)
		{
			_HoverNode.OnMouseHWheel(e.WithLocation((int)m_pt_in_canvas.X - _HoverNode.Left, (int)m_pt_in_canvas.Y - _HoverNode.Top));
		}
	}

	private void ProcessMouseClick(STNodeMouseEventArgs e)
	{
		if (_ActiveNode != null && m_is_process_mouse_event && PointInRectangle(_ActiveNode.Rectangle, m_pt_in_canvas.X, m_pt_in_canvas.Y))
		{
			_ActiveNode.OnMouseClick(e.WithLocation((int)m_pt_down_in_canvas.X - _ActiveNode.Left, (int)m_pt_down_in_canvas.Y - _ActiveNode.Top));
		}
	}

	protected override void OnKeyDown(WpfKeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (m_enableEdit && Keyboard.Modifiers == ModifierKeys.None)
		{
			int offsetX = 0;
			int offsetY = 0;
			switch (e.Key)
			{
			case Key.Left:
				offsetX = -10;
				break;
			case Key.Right:
				offsetX = 10;
				break;
			case Key.Up:
				offsetY = -10;
				break;
			case Key.Down:
				offsetY = 10;
				break;
			}
			if ((offsetX != 0 || offsetY != 0) && MoveSelectedNodes(offsetX, offsetY))
			{
				e.Handled = true;
				return;
			}
		}
		if (_ActiveNode != null)
		{
			_ActiveNode.OnKeyDown(e);
		}
	}

	protected override void OnKeyUp(WpfKeyEventArgs e)
	{
		base.OnKeyUp(e);
		if (_ActiveNode != null)
		{
			_ActiveNode.OnKeyUp(e);
		}
		m_node_down = null;
	}

	protected override void OnTextInput(WpfTextCompositionEventArgs e)
	{
		base.OnTextInput(e);
		if (_ActiveNode != null && !string.IsNullOrEmpty(e.Text))
		{
			var args = new STNodeKeyPressEventArgs(e.Text[0]);
			_ActiveNode.OnKeyPress(args);
			e.Handled = args.Handled;
		}
	}

	protected override void OnDragEnter(WpfDragEventArgs e)
	{
		base.OnDragEnter(e);
		if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
		{
			return;
		}
		e.Effects = e.Data.GetDataPresent("STNodeType")
			? System.Windows.DragDropEffects.Copy
			: System.Windows.DragDropEffects.None;
		e.Handled = true;
	}

	protected override void OnDrop(WpfDragEventArgs e)
	{
		base.OnDrop(e);
		if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this) || !e.Data.GetDataPresent("STNodeType"))
		{
			return;
		}
		if (e.Data.GetData("STNodeType") is Type type && type.IsSubclassOf(typeof(STNode)))
		{
			STNode node = (STNode)Activator.CreateInstance(type);
			node.Create();
			WpfPoint position = e.GetPosition(this);
			Point canvasPoint = ControlToCanvas(new Point((int)Math.Round(position.X), (int)Math.Round(position.Y)));
			node.Left = canvasPoint.X;
			node.Top = canvasPoint.Y;
			Nodes.Add(node);
			e.Handled = true;
		}
	}

	private STNodeMouseEventArgs CreateMouseEventArgs(WpfMouseEventArgs e)
	{
		WpfPoint position = e.GetPosition(this);
		STMouseButtons buttons;
		int clicks = 0;
		if (e is WpfMouseButtonEventArgs buttonEvent)
		{
			buttons = buttonEvent.ChangedButton switch
			{
				MouseButton.Left => STMouseButtons.Left,
				MouseButton.Right => STMouseButtons.Right,
				MouseButton.Middle => STMouseButtons.Middle,
				MouseButton.XButton1 => STMouseButtons.XButton1,
				MouseButton.XButton2 => STMouseButtons.XButton2,
				_ => STMouseButtons.None
			};
			clicks = buttonEvent.ClickCount;
		}
		else
		{
			buttons = STMouseButtons.None;
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				buttons |= STMouseButtons.Left;
			}
			if (e.RightButton == MouseButtonState.Pressed)
			{
				buttons |= STMouseButtons.Right;
			}
			if (e.MiddleButton == MouseButtonState.Pressed)
			{
				buttons |= STMouseButtons.Middle;
			}
			if (e.XButton1 == MouseButtonState.Pressed)
			{
				buttons |= STMouseButtons.XButton1;
			}
			if (e.XButton2 == MouseButtonState.Pressed)
			{
				buttons |= STMouseButtons.XButton2;
			}
		}
		int delta = e is WpfMouseWheelEventArgs wheelEvent ? wheelEvent.Delta : 0;
		return new STNodeMouseEventArgs(
			buttons,
			clicks,
			(int)Math.Round(position.X),
			(int)Math.Round(position.Y),
			delta);
	}

	protected virtual void OnDrawGrid(DrawingTools dt, int nWidth, int nHeight)
	{
		Graphics graphics = dt.Graphics;
		using Pen pen = new Pen(Color.FromArgb(65, _GridColor));
		using Pen pen2 = new Pen(Color.FromArgb(30, _GridColor));
		float num = 20f * _CanvasScale;
		int num2 = 5 - (int)(_CanvasOffsetX / num);
		for (float num3 = _CanvasOffsetX % num; num3 < (float)nWidth; num3 += num)
		{
			graphics.DrawLine((num2++ % 5 == 0) ? pen : pen2, num3, 0f, num3, nHeight);
		}
		num2 = 5 - (int)(_CanvasOffsetY / num);
		for (float num4 = _CanvasOffsetY % num; num4 < (float)nHeight; num4 += num)
		{
			graphics.DrawLine((num2++ % 5 == 0) ? pen : pen2, 0f, num4, nWidth, num4);
		}
		if (_HighlightGridOrigin)
		{
			pen2.Color = Color.FromArgb((_Nodes.Count == 0) ? 255 : 120, _GridColor);
			graphics.DrawLine(pen2, _CanvasOffsetX, 0f, _CanvasOffsetX, nHeight);
			graphics.DrawLine(pen2, 0f, _CanvasOffsetY, nWidth, _CanvasOffsetY);
		}
	}

	protected virtual void OnDrawNode(DrawingTools dt, Rectangle rect)
	{
		m_lst_node_out.Clear();
		foreach (STNode node in _Nodes)
		{
			if (!rect.IntersectsWith(node.Rectangle))
			{
				m_lst_node_out.Add(node.Location);
				continue;
			}
			if (_ShowBorder)
			{
				OnDrawNodeBorder(dt, node);
			}
			node.OnDrawNode(dt);
			if (!string.IsNullOrEmpty(node.Mark))
			{
				node.OnDrawMark(dt);
			}
			if (_ShowBorder)
			{
				OnDrawNodeSelection(dt, node);
			}
		}
	}

	protected virtual void OnDrawNodeBorder(DrawingTools dt, STNode node)
	{
		bool isActive = _ActiveNode == node;
		bool isSelected = node.IsSelected;
		bool isHovered = _HoverNode == node;
		if (!_ShowNodeShadow)
		{
			if (!isHovered || isActive || isSelected)
			{
				return;
			}
			DrawNodeOutline(dt.Graphics, node.Rectangle, _BorderHoverColor, 1f, inset: true);
			return;
		}

		Image image = isActive ? m_img_border_active : (isSelected ? m_img_border_selected : (isHovered ? m_img_border_hover : m_img_border));
		RenderBorder(dt.Graphics, node.Rectangle, image);
		if (!string.IsNullOrEmpty(node.Mark))
		{
			RenderBorder(dt.Graphics, node.MarkRectangle, image);
		}
	}

	protected virtual void OnDrawNodeSelection(DrawingTools dt, STNode node)
	{
		bool isActive = _ActiveNode == node;
		if (!isActive && !node.IsSelected)
		{
			return;
		}

		float canvasScale = Math.Max(_CanvasScale, 0.2f);
		Color outlineColor = isActive ? _BorderActiveColor : _BorderSelectedColor;
		float outlineWidth = (isActive ? 2f : 1.5f) / canvasScale;
		DrawNodeOutline(dt.Graphics, node.Rectangle, outlineColor, outlineWidth, inset: true);
	}

	private void DrawNodeOutline(Graphics graphics, Rectangle rectangle, Color color, float width, bool inset)
	{
		GraphicsState graphicsState = graphics.Save();
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		float pathInset = inset ? width / 2f : 0f;
		RectangleF outlineRectangle = new RectangleF(
			rectangle.Left + pathInset,
			rectangle.Top + pathInset,
			Math.Max(0f, rectangle.Width - pathInset * 2f),
			Math.Max(0f, rectangle.Height - pathInset * 2f));
		using GraphicsPath outlinePath = CreateRoundedRectanglePath(outlineRectangle, _NodeCornerRadius);
		using Pen outlinePen = new Pen(color, width);
		graphics.DrawPath(outlinePen, outlinePath);
		graphics.Restore(graphicsState);
	}

	private static GraphicsPath CreateRoundedRectanglePath(RectangleF rectangle, float radius)
	{
		GraphicsPath path = new GraphicsPath();
		float maxRadius = Math.Min(rectangle.Width, rectangle.Height) / 2f;
		radius = Math.Min(radius, maxRadius);
		if (radius <= 0f)
		{
			path.AddRectangle(rectangle);
			return path;
		}

		float diameter = radius * 2f;
		path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180f, 90f);
		path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270f, 90f);
		path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0f, 90f);
		path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90f, 90f);
		path.CloseFigure();
		return path;
	}

	protected virtual void OnDrawConnectedLine(DrawingTools dt)
	{
		Graphics graphics = dt.Graphics;
		graphics.SmoothingMode = SmoothingMode.HighQuality;
		m_p_line_hover.Color = Color.FromArgb(10, 0, 0, 0);
		Type typeFromHandle = typeof(object);
		foreach (STNode node in _Nodes)
		{
			foreach (STNodeOption outputOption in node.OutputOptions)
			{
				if (outputOption == STNodeOption.Empty)
				{
					continue;
				}
				if (outputOption.DotColor != Color.Transparent)
				{
					m_p_line.Color = outputOption.DotColor;
				}
				else if (outputOption.DataType == typeFromHandle)
				{
					m_p_line.Color = _UnknownTypeColor;
				}
				else
				{
					m_p_line.Color = (_TypeColor.ContainsKey(outputOption.DataType) ? _TypeColor[outputOption.DataType] : _UnknownTypeColor);
				}
				foreach (STNodeOption item in outputOption.ConnectedOption)
				{
					float x1 = outputOption.DotLeft + outputOption.DotSize;
					float y1 = outputOption.DotTop + outputOption.DotSize / 2;
					float x2 = item.DotLeft - 1;
					float y2 = item.DotTop + item.DotSize / 2;
					DrawBezier(graphics, m_p_line_hover, x1, y1, x2, y2, _Curvature);
					DrawBezier(graphics, m_p_line, x1, y1, x2, y2, _Curvature);
					if (m_is_buildpath)
					{
						GraphicsPath key = CreateBezierPath(x1, y1, x2, y2, _Curvature);
						m_dic_gp_info.Add(key, new ConnectionInfo
						{
							Output = outputOption,
							Input = item
						});
					}
				}
			}
		}
		m_p_line_hover.Color = _HighLineColor;
		if (m_gp_hover != null && m_dic_gp_info.ContainsKey(m_gp_hover))
		{
			graphics.DrawPath(m_p_line_hover, m_gp_hover);
		}
		else
		{
			m_gp_hover = null;
		}
		m_is_buildpath = false;
	}

	protected virtual void OnDrawMark(DrawingTools dt)
	{
		Graphics graphics = dt.Graphics;
		SizeF sizeF = graphics.MeasureString(m_find.Mark, Font);
		Rectangle rectangle = new Rectangle(m_pt_in_control.X + 15, m_pt_in_control.Y + 10, (int)sizeF.Width + 6, 4 + (Font.Height + 4) * m_find.MarkLines.Length);
		if (rectangle.Right > ClientSize.Width)
		{
			rectangle.X = ClientSize.Width - rectangle.Width;
		}
		if (rectangle.Bottom > ClientSize.Height)
		{
			rectangle.Y = ClientSize.Height - rectangle.Height;
		}
		if (rectangle.X < 0)
		{
			rectangle.X = 0;
		}
		if (rectangle.Y < 0)
		{
			rectangle.Y = 0;
		}
		dt.SolidBrush.Color = _MarkBackColor;
		graphics.SmoothingMode = SmoothingMode.None;
		graphics.FillRectangle(dt.SolidBrush, rectangle);
		rectangle.Width--;
		rectangle.Height--;
		dt.Pen.Color = Color.FromArgb(255, _MarkBackColor);
		graphics.DrawRectangle(dt.Pen, rectangle);
		dt.SolidBrush.Color = _MarkForeColor;
		m_sf.LineAlignment = StringAlignment.Center;
		rectangle.X += 2;
		rectangle.Width -= 3;
		rectangle.Height = Font.Height + 4;
		int num = rectangle.Y + 2;
		for (int i = 0; i < m_find.MarkLines.Length; i++)
		{
			rectangle.Y = num + i * (Font.Height + 4);
			graphics.DrawString(m_find.MarkLines[i], Font, dt.SolidBrush, rectangle, m_sf);
		}
	}

	protected virtual void OnDrawMagnet(DrawingTools dt, MagnetInfo mi)
	{
		if (_ActiveNode == null)
		{
			return;
		}
		Graphics graphics = dt.Graphics;
		GraphicsState state = graphics.Save();
		Pen pen = m_drawing_tools.Pen;
		SolidBrush solidBrush = dt.SolidBrush;
		pen.Color = _MagnetColor;
		solidBrush.Color = Color.FromArgb(_MagnetColor.A / 3, _MagnetColor);
		graphics.SmoothingMode = SmoothingMode.None;
		int left = _ActiveNode.Left;
		int num = _ActiveNode.Left + _ActiveNode.Width / 2;
		int right = _ActiveNode.Right;
		int top = _ActiveNode.Top;
		int num2 = _ActiveNode.Top + _ActiveNode.Height / 2;
		int bottom = _ActiveNode.Bottom;
		if (mi.XMatched)
		{
			graphics.DrawLine(pen, CanvasToControl(mi.X, isX: true), 0f, CanvasToControl(mi.X, isX: true), ClientSize.Height);
		}
		if (mi.YMatched)
		{
			graphics.DrawLine(pen, 0f, CanvasToControl(mi.Y, isX: false), ClientSize.Width, CanvasToControl(mi.Y, isX: false));
		}
		graphics.TranslateTransform(_CanvasOffsetX, _CanvasOffsetY);
		graphics.ScaleTransform(_CanvasScale, _CanvasScale);
		if (mi.XMatched)
		{
			foreach (STNode node in _Nodes)
			{
				if (node.Left == mi.X || node.Right == mi.X || node.Left + node.Width / 2 == mi.X)
				{
					graphics.FillRectangle(solidBrush, node.Rectangle);
				}
			}
		}
		if (mi.YMatched)
		{
			foreach (STNode node2 in _Nodes)
			{
				if (node2.Top == mi.Y || node2.Bottom == mi.Y || node2.Top + node2.Height / 2 == mi.Y)
				{
					graphics.FillRectangle(solidBrush, node2.Rectangle);
				}
			}
		}
		graphics.Restore(state);
	}

	protected virtual void OnDrawSelectedRectangle(DrawingTools dt, RectangleF rectf)
	{
		Graphics graphics = dt.Graphics;
		SolidBrush solidBrush = dt.SolidBrush;
		dt.Pen.Color = _SelectedRectangleColor;
		graphics.DrawRectangle(dt.Pen, rectf.Left, rectf.Y, rectf.Width, rectf.Height);
		solidBrush.Color = Color.FromArgb(_SelectedRectangleColor.A / 3, _SelectedRectangleColor);
		graphics.FillRectangle(solidBrush, CanvasToControl(m_rect_select));
	}

	protected virtual void OnDrawNodeOutLocation(DrawingTools dt, Size sz, List<Point> lstPts)
	{
		Graphics graphics = dt.Graphics;
		SolidBrush solidBrush = dt.SolidBrush;
		solidBrush.Color = _LocationBackColor;
		graphics.SmoothingMode = SmoothingMode.None;
		if (lstPts.Count == _Nodes.Count && _Nodes.Count != 0)
		{
			graphics.FillRectangle(solidBrush, CanvasToControl(_CanvasValidBounds));
		}
		graphics.FillRectangle(solidBrush, 0, 0, 4, sz.Height);
		graphics.FillRectangle(solidBrush, sz.Width - 4, 0, 4, sz.Height);
		graphics.FillRectangle(solidBrush, 4, 0, sz.Width - 8, 4);
		graphics.FillRectangle(solidBrush, 4, sz.Height - 4, sz.Width - 8, 4);
		solidBrush.Color = _LocationForeColor;
		foreach (Point lstPt in lstPts)
		{
			Point point = CanvasToControl(lstPt);
			if (point.X < 0)
			{
				point.X = 0;
			}
			if (point.Y < 0)
			{
				point.Y = 0;
			}
			if (point.X > sz.Width)
			{
				point.X = sz.Width - 4;
			}
			if (point.Y > sz.Height)
			{
				point.Y = sz.Height - 4;
			}
			graphics.FillRectangle(solidBrush, point.X, point.Y, 4, 4);
		}
	}

	protected virtual void OnDrawAlert(DrawingTools dt, Rectangle rect, string strText, Color foreColor, Color backColor, AlertLocation al)
	{
		if (m_alpha_alert != 0)
		{
			Graphics graphics = dt.Graphics;
			SolidBrush solidBrush = dt.SolidBrush;
			graphics.SmoothingMode = SmoothingMode.None;
			solidBrush.Color = backColor;
			dt.Pen.Color = solidBrush.Color;
			graphics.FillRectangle(solidBrush, rect);
			graphics.DrawRectangle(dt.Pen, rect.Left, rect.Top, rect.Width - 1, rect.Height - 1);
			solidBrush.Color = foreColor;
			m_sf.Alignment = StringAlignment.Center;
			m_sf.LineAlignment = StringAlignment.Center;
			graphics.SmoothingMode = SmoothingMode.HighQuality;
			graphics.DrawString(strText, Font, solidBrush, rect, m_sf);
		}
	}

	protected virtual void OnDrawCanvasDragLockButton(DrawingTools dt)
	{
		const int size = 28;
		const int margin = 8;
		m_rect_canvas_drag_lock = new Rectangle(ClientSize.Width - size - margin, margin, size, size);

		Graphics graphics = dt.Graphics;
		Color backColor = EnableBlankLeftDragCanvas
			? Color.FromArgb(220, 40, 120, 60)
			: Color.FromArgb(190, 35, 35, 35);
		Color borderColor = EnableBlankLeftDragCanvas
			? Color.FromArgb(240, 92, 196, 112)
			: Color.FromArgb(220, 110, 110, 110);

		using SolidBrush backgroundBrush = new SolidBrush(backColor);
		using Pen borderPen = new Pen(borderColor);
		graphics.FillRectangle(backgroundBrush, m_rect_canvas_drag_lock);
		graphics.DrawRectangle(borderPen, m_rect_canvas_drag_lock);

		using Font iconFont = new Font("Segoe MDL2 Assets", 13f, System.Drawing.FontStyle.Regular, GraphicsUnit.Point);
		using SolidBrush iconBrush = new SolidBrush(Color.White);
		using StringFormat iconFormat = new StringFormat
		{
			Alignment = StringAlignment.Center,
			LineAlignment = StringAlignment.Center
		};
		graphics.DrawString(EnableBlankLeftDragCanvas ? "\uE72E" : "\uE785", iconFont, iconBrush, m_rect_canvas_drag_lock, iconFormat);
	}

	protected virtual Rectangle GetAlertRectangle(Graphics g, string strText, AlertLocation al)
	{
		SizeF sizeF = g.MeasureString(m_str_alert, Font);
		Size size = new Size((int)Math.Round(sizeF.Width + 10f), (int)Math.Round(sizeF.Height + 4f));
		Rectangle result = new Rectangle(4, ClientSize.Height - size.Height - 4, size.Width, size.Height);
		switch (al)
		{
		case AlertLocation.Left:
			result.Y = ClientSize.Height - size.Height >> 1;
			break;
		case AlertLocation.Top:
			result.Y = 4;
			result.X = ClientSize.Width - size.Width >> 1;
			break;
		case AlertLocation.Right:
			result.X = ClientSize.Width - size.Width - 4;
			result.Y = ClientSize.Height - size.Height >> 1;
			break;
		case AlertLocation.Bottom:
			result.X = ClientSize.Width - size.Width >> 1;
			break;
		case AlertLocation.Center:
			result.X = ClientSize.Width - size.Width >> 1;
			result.Y = ClientSize.Height - size.Height >> 1;
			break;
		case AlertLocation.LeftTop:
		{
			int num = (result.Y = 4);
			result.X = num;
			break;
		}
		case AlertLocation.RightTop:
			result.Y = 4;
			result.X = ClientSize.Width - size.Width - 4;
			break;
		case AlertLocation.RightBottom:
			result.X = ClientSize.Width - size.Width - 4;
			break;
		}
		return result;
	}

	internal void BuildLinePath()
	{
		m_gp_hover = null;
		foreach (KeyValuePair<GraphicsPath, ConnectionInfo> item in m_dic_gp_info)
		{
			item.Key.Dispose();
		}
		m_dic_gp_info.Clear();
		m_is_buildpath = true;
		Invalidate();
	}

	internal void OnDrawAlert(Graphics g)
	{
		m_rect_alert = GetAlertRectangle(g, m_str_alert, m_al);
		Color foreColor = Color.FromArgb((int)((float)m_alpha_alert / 255f * (float)(int)m_forecolor_alert.A), m_forecolor_alert);
		Color backColor = Color.FromArgb((int)((float)m_alpha_alert / 255f * (float)(int)m_backcolor_alert.A), m_backcolor_alert);
		OnDrawAlert(m_drawing_tools, m_rect_alert, m_str_alert, foreColor, backColor, m_al);
	}

	internal void InternalAddSelectedNode(STNode node)
	{
		node.IsSelected = true;
		lock (m_hs_node_selected)
		{
			m_hs_node_selected.Add(node);
		}
		UpdateCanvasDragModeFromSelection();
	}

	internal void InternalRemoveSelectedNode(STNode node)
	{
		node.IsSelected = false;
		lock (m_hs_node_selected)
		{
			m_hs_node_selected.Remove(node);
		}
		UpdateCanvasDragModeFromSelection();
	}

	private Image CreateBorderImage(Color clr)
	{
		Image image = new Bitmap(12, 12);
		using Graphics graphics = Graphics.FromImage(image);
		graphics.SmoothingMode = SmoothingMode.HighQuality;
		using GraphicsPath graphicsPath = new GraphicsPath();
		graphicsPath.AddEllipse(new Rectangle(0, 0, 11, 11));
		using PathGradientBrush pathGradientBrush = new PathGradientBrush(graphicsPath);
		pathGradientBrush.CenterColor = Color.FromArgb(200, clr);
		pathGradientBrush.SurroundColors = new Color[1] { Color.FromArgb(10, clr) };
		graphics.FillPath(pathGradientBrush, graphicsPath);
		return image;
	}

	private ConnectionStatus DisConnectionHover()
	{
		GraphicsPath gpHover = m_gp_hover;
		if (gpHover == null || !m_dic_gp_info.TryGetValue(gpHover, out ConnectionInfo connectionInfo))
		{
			return ConnectionStatus.DisConnected;
		}
		ConnectionStatus connectionStatus = connectionInfo.Output.DisConnectOption(connectionInfo.Input);
		if (connectionStatus == ConnectionStatus.DisConnected)
		{
			m_dic_gp_info.Remove(gpHover);
			gpHover.Dispose();
			if (ReferenceEquals(m_gp_hover, gpHover))
			{
				m_gp_hover = null;
			}
			Invalidate();
		}
		return connectionStatus;
	}

	private void StartConnect(STNodeOption op)
	{
		if (op.IsInput)
		{
			m_pt_dot_down.X = op.DotLeft;
			m_pt_dot_down.Y = op.DotTop + 5;
		}
		else
		{
			m_pt_dot_down.X = op.DotLeft + op.DotSize;
			m_pt_dot_down.Y = op.DotTop + 5;
		}
		m_ca = CanvasAction.ConnectOption;
		m_option_down = op;
		BeginPointerEdit("连接节点");
	}

	public void AlignTop()
	{
		if (m_hs_node_selected.Count <= 1)
		{
			return;
		}
		using STNodeEditTransaction transaction = BeginEditTransaction("顶部对齐");
		STNode sTNode = m_hs_node_selected.First();
		lock (m_hs_node_selected)
		{
			foreach (STNode item in m_hs_node_selected)
			{
				if (item != sTNode)
				{
					item.Top = sTNode.Top;
				}
			}
		}
	}

	public void AlignLeft()
	{
		if (m_hs_node_selected.Count <= 1)
		{
			return;
		}
		using STNodeEditTransaction transaction = BeginEditTransaction("左对齐");
		STNode sTNode = m_hs_node_selected.First();
		lock (m_hs_node_selected)
		{
			foreach (STNode item in m_hs_node_selected)
			{
				if (item != sTNode)
				{
					item.Left = sTNode.Left;
				}
			}
		}
	}

	public void AlignVerticalCenter()
	{
		if (m_hs_node_selected.Count <= 1)
		{
			return;
		}
		using STNodeEditTransaction transaction = BeginEditTransaction("垂直居中");
		STNode sTNode = m_hs_node_selected.First();
		int num = sTNode.Left + sTNode.Width / 2;
		lock (m_hs_node_selected)
		{
			foreach (STNode item in m_hs_node_selected)
			{
				if (item != sTNode)
				{
					item.Left = num - item.Width / 2;
				}
			}
		}
	}

	public void AlignHorizontalCenter()
	{
		if (m_hs_node_selected.Count <= 1)
		{
			return;
		}
		using STNodeEditTransaction transaction = BeginEditTransaction("水平居中");
		STNode sTNode = m_hs_node_selected.First();
		int num = sTNode.Top + sTNode.Height / 2;
		lock (m_hs_node_selected)
		{
			foreach (STNode item in m_hs_node_selected)
			{
				if (item != sTNode)
				{
					item.Top = num - item.Height / 2;
				}
			}
		}
	}

	public void AlignHorizontalDistance()
	{
		if (m_hs_node_selected.Count <= 1)
		{
			return;
		}
		using STNodeEditTransaction transaction = BeginEditTransaction("水平等距");
		List<STNode> source = m_hs_node_selected.ToList();
		int num = source.Sum((STNode x) => x.Width);
		List<STNode> list = source.OrderBy((STNode p) => p.Left).ToList();
		int num2 = list.Last().Right - list.First().Left;
		int num3 = (num2 - num) / (list.Count - 1);
		if (num3 < 50)
		{
			num3 = 50;
		}
		lock (m_hs_node_selected)
		{
			for (int num4 = 1; num4 < list.Count; num4++)
			{
				STNode sTNode = list[num4];
				STNode sTNode2 = list[num4 - 1];
				sTNode.Left = sTNode2.Left + sTNode2.Width + num3;
			}
		}
	}

	public void AlignVerticalDistance()
	{
		if (m_hs_node_selected.Count <= 1)
		{
			return;
		}
		using STNodeEditTransaction transaction = BeginEditTransaction("垂直等距");
		List<STNode> source = m_hs_node_selected.ToList();
		int num = source.Sum((STNode x) => x.Height);
		List<STNode> list = source.OrderBy((STNode p) => p.Top).ToList();
		int num2 = list.Last().Bottom - list.First().Top;
		int num3 = (num2 - num) / (list.Count - 1);
		if (num3 < 20)
		{
			num3 = 20;
		}
		lock (m_hs_node_selected)
		{
			for (int num4 = 1; num4 < list.Count; num4++)
			{
				STNode sTNode = list[num4];
				STNode sTNode2 = list[num4 - 1];
				sTNode.Top = sTNode2.Top + sTNode2.Height + num3;
			}
		}
	}

	private void MoveNode(Point pt)
	{
		int num = (int)((float)(pt.X - m_pt_down_in_control.X) / _CanvasScale);
		int num2 = (int)((float)(pt.Y - m_pt_down_in_control.Y) / _CanvasScale);
		lock (m_hs_node_selected)
		{
			foreach (STNode item in m_hs_node_selected)
			{
				item.Left = m_dic_pt_selected[item].X + num;
				item.Top = m_dic_pt_selected[item].Y + num2;
			}
			if (_ShowMagnet)
			{
				MagnetInfo magnetInfo = CheckMagnet(_ActiveNode);
				if (magnetInfo.XMatched)
				{
					foreach (STNode item2 in m_hs_node_selected)
					{
						item2.Left -= magnetInfo.OffsetX;
					}
				}
				if (magnetInfo.YMatched)
				{
					foreach (STNode item3 in m_hs_node_selected)
					{
						item3.Top -= magnetInfo.OffsetY;
					}
				}
			}
		}
		Invalidate();
	}

	protected internal virtual void BuildBounds()
	{
		if (_Nodes.Count == 0)
		{
			_CanvasValidBounds = ControlToCanvas(ClientRectangle);
			return;
		}
		int num = int.MaxValue;
		int num2 = int.MaxValue;
		int num3 = int.MinValue;
		int num4 = int.MinValue;
		foreach (STNode node in _Nodes)
		{
			if (num > node.Left)
			{
				num = node.Left;
			}
			if (num2 > node.Top)
			{
				num2 = node.Top;
			}
			if (num3 < node.Right)
			{
				num3 = node.Right;
			}
			if (num4 < node.Bottom)
			{
				num4 = node.Bottom;
			}
		}
		_CanvasValidBounds.X = num - 60;
		_CanvasValidBounds.Y = num2 - 60;
		_CanvasValidBounds.Width = num3 - num + 120;
		_CanvasValidBounds.Height = num4 - num2 + 120;
	}

	private bool PointInRectangle(Rectangle rect, float x, float y)
	{
		if (x < (float)rect.Left)
		{
			return false;
		}
		if (x > (float)rect.Right)
		{
			return false;
		}
		if (y < (float)rect.Top)
		{
			return false;
		}
		if (y > (float)rect.Bottom)
		{
			return false;
		}
		return true;
	}

	private void BuildMagnetLocation()
	{
		m_lst_magnet_x.Clear();
		m_lst_magnet_y.Clear();
		foreach (STNode node in _Nodes)
		{
			if (!node.IsSelected)
			{
				m_lst_magnet_x.Add(node.Left);
				m_lst_magnet_x.Add(node.Left + node.Width / 2);
				m_lst_magnet_x.Add(node.Left + node.Width);
				m_lst_magnet_y.Add(node.Top);
				m_lst_magnet_y.Add(node.Top + node.Height / 2);
				m_lst_magnet_y.Add(node.Top + node.Height);
			}
		}
	}

	private MagnetInfo CheckMagnet(STNode node)
	{
		m_mi.XMatched = (m_mi.YMatched = false);
		m_lst_magnet_mx.Clear();
		m_lst_magnet_my.Clear();
		m_lst_magnet_mx.Add(node.Left + node.Width / 2);
		m_lst_magnet_mx.Add(node.Left);
		m_lst_magnet_mx.Add(node.Left + node.Width);
		m_lst_magnet_my.Add(node.Top + node.Height / 2);
		m_lst_magnet_my.Add(node.Top);
		m_lst_magnet_my.Add(node.Top + node.Height);
		bool flag = false;
		foreach (int item in m_lst_magnet_mx)
		{
			foreach (int item2 in m_lst_magnet_x)
			{
				if (Math.Abs(item - item2) <= 5)
				{
					flag = true;
					m_mi.X = item2;
					m_mi.OffsetX = item - item2;
					m_mi.XMatched = true;
					break;
				}
			}
			if (flag)
			{
				break;
			}
		}
		flag = false;
		foreach (int item3 in m_lst_magnet_my)
		{
			foreach (int item4 in m_lst_magnet_y)
			{
				if (Math.Abs(item3 - item4) <= 5)
				{
					flag = true;
					m_mi.Y = item4;
					m_mi.OffsetY = item3 - item4;
					m_mi.YMatched = true;
					break;
				}
			}
			if (flag)
			{
				break;
			}
		}
		return m_mi;
	}

	private void DrawBezier(Graphics g, Pen p, PointF ptStart, PointF ptEnd, float f)
	{
		DrawBezier(g, p, ptStart.X, ptStart.Y, ptEnd.X, ptEnd.Y, f);
	}

	private void DrawBezier(Graphics g, Pen p, float x1, float y1, float x2, float y2, float f)
	{
		using GraphicsPath connectionPath = CreateBezierPath(x1, y1, x2, y2, f);
		g.DrawPath(p, connectionPath);
	}

	protected internal static GraphicsPath CreateBezierPath(float x1, float y1, float x2, float y2, float f)
	{
		GraphicsPath graphicsPath = new GraphicsPath();
		if (x2 < x1 && Math.Abs(y2 - y1) < 60f)
		{
			float routeY = Math.Max(y1, y2) + 70f;
			float startGutterX = x1 + 30f;
			float endGutterX = x2 - 30f;
			float midpointX = (x1 + x2) / 2f;
			graphicsPath.AddBezier(x1, y1, startGutterX, y1, startGutterX, routeY, midpointX, routeY);
			graphicsPath.AddBezier(midpointX, routeY, endGutterX, routeY, endGutterX, y2, x2, y2);
			return graphicsPath;
		}

		float num = Math.Abs(x1 - x2) * f;
		if (f != 0f && num < 30f)
		{
			num = 30f;
		}
		graphicsPath.AddBezier(x1, y1, x1 + num, y1, x2 - num, y2, x2, y2);
		return graphicsPath;
	}

	private void RenderBorder(Graphics g, Rectangle rect, Image img)
	{
		g.DrawImage(img, new Rectangle(rect.X - 5, rect.Y - 5, 5, 5), new Rectangle(0, 0, 5, 5), GraphicsUnit.Pixel);
		g.DrawImage(img, new Rectangle(rect.Right, rect.Y - 5, 5, 5), new Rectangle(img.Width - 5, 0, 5, 5), GraphicsUnit.Pixel);
		g.DrawImage(img, new Rectangle(rect.X - 5, rect.Bottom, 5, 5), new Rectangle(0, img.Height - 5, 5, 5), GraphicsUnit.Pixel);
		g.DrawImage(img, new Rectangle(rect.Right, rect.Bottom, 5, 5), new Rectangle(img.Width - 5, img.Height - 5, 5, 5), GraphicsUnit.Pixel);
		g.DrawImage(img, new Rectangle(rect.X - 5, rect.Y, 5, rect.Height), new Rectangle(0, 5, 5, img.Height - 10), GraphicsUnit.Pixel);
		g.DrawImage(img, new Rectangle(rect.X, rect.Y - 5, rect.Width, 5), new Rectangle(5, 0, img.Width - 10, 5), GraphicsUnit.Pixel);
		g.DrawImage(img, new Rectangle(rect.Right, rect.Y, 5, rect.Height), new Rectangle(img.Width - 5, 5, 5, img.Height - 10), GraphicsUnit.Pixel);
		g.DrawImage(img, new Rectangle(rect.X, rect.Bottom, rect.Width, 5), new Rectangle(5, img.Height - 5, img.Width - 10, 5), GraphicsUnit.Pixel);
	}

	public void Invalidate()
	{
		if (m_disposed || Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
		{
			return;
		}
		if (Dispatcher.CheckAccess())
		{
			InvalidateVisual();
			return;
		}
		_ = Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(InvalidateVisual));
	}

	public void Invalidate(Rectangle rectangle)
	{
		Invalidate();
	}

	public Graphics CreateGraphics()
	{
		if (m_disposed)
		{
			throw new ObjectDisposedException(nameof(STNodeEditor));
		}
		return Graphics.FromImage(m_measurement_bitmap);
	}

	public IAsyncResult BeginInvoke(Delegate method)
	{
		return BeginInvoke(method, null);
	}

	public IAsyncResult BeginInvoke(Delegate method, params object[] args)
	{
		if (method == null
			|| m_disposed
			|| Dispatcher.HasShutdownStarted
			|| Dispatcher.HasShutdownFinished)
		{
			return null;
		}
		try
		{
			DispatcherOperation operation = Dispatcher.BeginInvoke(
				DispatcherPriority.Normal,
				new Action(() =>
				{
					if (!m_disposed)
					{
						method.DynamicInvoke(args ?? Array.Empty<object>());
					}
				}));
			return operation.Task;
		}
		catch (InvalidOperationException) when (
			m_disposed
			|| Dispatcher.HasShutdownStarted
			|| Dispatcher.HasShutdownFinished)
		{
			return null;
		}
	}

	public object Invoke(Delegate method)
	{
		return Invoke(method, null);
	}

	public object Invoke(Delegate method, params object[] args)
	{
		if (method == null || m_disposed)
		{
			return null;
		}
		if (Dispatcher.CheckAccess())
		{
			return method.DynamicInvoke(args ?? Array.Empty<object>());
		}
		return Dispatcher.Invoke(() => method.DynamicInvoke(args ?? Array.Empty<object>()));
	}

	public Point PointToClient(Point point)
	{
		try
		{
			WpfPoint result = PointFromScreen(new WpfPoint(point.X, point.Y));
			return new Point((int)Math.Round(result.X), (int)Math.Round(result.Y));
		}
		catch (InvalidOperationException)
		{
			return point;
		}
	}

	public Point PointToScreen(Point point)
	{
		try
		{
			WpfPoint result = base.PointToScreen(new WpfPoint(point.X, point.Y));
			return new Point((int)Math.Round(result.X), (int)Math.Round(result.Y));
		}
		catch (InvalidOperationException)
		{
			return point;
		}
	}

	public Rectangle RectangleToScreen(Rectangle rectangle)
	{
		Point topLeft = PointToScreen(rectangle.Location);
		Point bottomRight = PointToScreen(new Point(rectangle.Right, rectangle.Bottom));
		return Rectangle.FromLTRB(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
	}

	public NodeFindInfo FindNodeFromPoint(PointF pt)
	{
		return FindNodeFromPoint(pt, 6);
	}

	public NodeFindInfo FindNodeFromPoint(PointF pt, int dotPadding)
	{
		m_find.Node = null;
		m_find.NodeOption = null;
		m_find.Mark = null;
		for (int num = _Nodes.Count - 1; num >= 0; num--)
		{
			if (!string.IsNullOrEmpty(_Nodes[num].Mark) && PointInRectangle(_Nodes[num].MarkRectangle, pt.X, pt.Y))
			{
				m_find.Mark = _Nodes[num].Mark;
				m_find.MarkLines = _Nodes[num].MarkLines;
				return m_find;
			}
			foreach (STNodeOption inputOption in _Nodes[num].InputOptions)
			{
				if (inputOption != STNodeOption.Empty && PointInRectangle(Rectangle.Inflate(inputOption.DotRectangle, dotPadding, dotPadding), pt.X, pt.Y))
				{
					m_find.NodeOption = inputOption;
				}
			}
			foreach (STNodeOption outputOption in _Nodes[num].OutputOptions)
			{
				if (outputOption != STNodeOption.Empty && PointInRectangle(Rectangle.Inflate(outputOption.DotRectangle, dotPadding, dotPadding), pt.X, pt.Y))
				{
					m_find.NodeOption = outputOption;
				}
			}
			if (PointInRectangle(_Nodes[num].Rectangle, pt.X, pt.Y))
			{
				m_find.Node = _Nodes[num];
			}
			if (m_find.NodeOption != null || m_find.Node != null)
			{
				return m_find;
			}
		}
		return m_find;
	}

	public STNode[] GetSelectedNode()
	{
		return m_hs_node_selected.ToArray();
	}

	public float CanvasToControl(float number, bool isX)
	{
		return number * _CanvasScale + (isX ? _CanvasOffsetX : _CanvasOffsetY);
	}

	public PointF CanvasToControl(PointF pt)
	{
		pt.X = pt.X * _CanvasScale + _CanvasOffsetX;
		pt.Y = pt.Y * _CanvasScale + _CanvasOffsetY;
		return pt;
	}

	public Point CanvasToControl(Point pt)
	{
		pt.X = (int)((float)pt.X * _CanvasScale + _CanvasOffsetX);
		pt.Y = (int)((float)pt.Y * _CanvasScale + _CanvasOffsetY);
		return pt;
	}

	public Rectangle CanvasToControl(Rectangle rect)
	{
		rect.X = (int)((float)rect.X * _CanvasScale + _CanvasOffsetX);
		rect.Y = (int)((float)rect.Y * _CanvasScale + _CanvasOffsetY);
		rect.Width = (int)((float)rect.Width * _CanvasScale);
		rect.Height = (int)((float)rect.Height * _CanvasScale);
		return rect;
	}

	public RectangleF CanvasToControl(RectangleF rect)
	{
		rect.X = rect.X * _CanvasScale + _CanvasOffsetX;
		rect.Y = rect.Y * _CanvasScale + _CanvasOffsetY;
		rect.Width *= _CanvasScale;
		rect.Height *= _CanvasScale;
		return rect;
	}

	public float ControlToCanvas(float number, bool isX)
	{
		return (number - (isX ? _CanvasOffsetX : _CanvasOffsetY)) / _CanvasScale;
	}

	public Point ControlToCanvas(Point pt)
	{
		pt.X = (int)(((float)pt.X - _CanvasOffsetX) / _CanvasScale);
		pt.Y = (int)(((float)pt.Y - _CanvasOffsetY) / _CanvasScale);
		return pt;
	}

	public PointF ControlToCanvas(PointF pt)
	{
		pt.X = (pt.X - _CanvasOffsetX) / _CanvasScale;
		pt.Y = (pt.Y - _CanvasOffsetY) / _CanvasScale;
		return pt;
	}

	public Rectangle ControlToCanvas(Rectangle rect)
	{
		rect.X = (int)(((float)rect.X - _CanvasOffsetX) / _CanvasScale);
		rect.Y = (int)(((float)rect.Y - _CanvasOffsetY) / _CanvasScale);
		rect.Width = (int)((float)rect.Width / _CanvasScale);
		rect.Height = (int)((float)rect.Height / _CanvasScale);
		return rect;
	}

	public RectangleF ControlToCanvas(RectangleF rect)
	{
		rect.X = (rect.X - _CanvasOffsetX) / _CanvasScale;
		rect.Y = (rect.Y - _CanvasOffsetY) / _CanvasScale;
		rect.Width /= _CanvasScale;
		rect.Height /= _CanvasScale;
		return rect;
	}

	public void MoveCanvas(float x, float y, bool bAnimation, CanvasMoveArgs ma)
	{
		if (_LimitCanvasToContentBounds && _Nodes.Count == 0)
		{
			m_real_canvas_x = (m_real_canvas_y = 10f);
			UpdateAnimationTimerState();
			return;
		}
		if (_LimitCanvasToContentBounds)
		{
			int num = (int)((float)(_CanvasValidBounds.Left + 50) * _CanvasScale);
			int num2 = (int)((float)(_CanvasValidBounds.Top + 50) * _CanvasScale);
			int num3 = (int)((float)(_CanvasValidBounds.Right - 50) * _CanvasScale);
			int num4 = (int)((float)(_CanvasValidBounds.Bottom - 50) * _CanvasScale);
			if ((float)num3 + x < 0f)
			{
				x = -num3;
			}
			if ((float)(ClientSize.Width - num) < x)
			{
				x = ClientSize.Width - num;
			}
			if ((float)num4 + y < 0f)
			{
				y = -num4;
			}
			if ((float)(ClientSize.Height - num2) < y)
			{
				y = ClientSize.Height - num2;
			}
		}
		if (bAnimation)
		{
			bool moveAll = ma == CanvasMoveArgs.All;
			if (moveAll || (ma & CanvasMoveArgs.Left) == CanvasMoveArgs.Left)
			{
				m_real_canvas_x = x;
			}
			if (moveAll || (ma & CanvasMoveArgs.Top) == CanvasMoveArgs.Top)
			{
				m_real_canvas_y = y;
			}
		}
		else
		{
			m_real_canvas_x = (_CanvasOffsetX = x);
			m_real_canvas_y = (_CanvasOffsetY = y);
			Invalidate();
		}
		UpdateAnimationTimerState();
		OnCanvasMoved(EventArgs.Empty);
	}

	public void ScaleCanvas(float f, float x, float y)
	{
		if (_LimitCanvasToContentBounds && _Nodes.Count == 0)
		{
			_CanvasScale = 1f;
		}
		else if (_CanvasScale != f)
		{
			if ((double)f < 0.2)
			{
				f = 0.2f;
			}
			else if (f > 5f)
			{
				f = 5f;
			}
			float number = ControlToCanvas(x, isX: true);
			float number2 = ControlToCanvas(y, isX: false);
			_CanvasScale = f;
			_CanvasOffsetX = (m_real_canvas_x -= CanvasToControl(number, isX: true) - x);
			_CanvasOffsetY = (m_real_canvas_y -= CanvasToControl(number2, isX: false) - y);
			OnCanvasScaled(EventArgs.Empty);
			Invalidate();
		}
	}

	public void FitCanvasToNodes(float maximumScale = 1f)
	{
		if (_Nodes.Count == 0 || ClientSize.Width <= 0 || ClientSize.Height <= 0 || _CanvasValidBounds.Width <= 0 || _CanvasValidBounds.Height <= 0)
		{
			return;
		}
		float scaleX = (float)ClientSize.Width / _CanvasValidBounds.Width;
		float scaleY = (float)ClientSize.Height / _CanvasValidBounds.Height;
		float scale = Math.Min(Math.Min(scaleX, scaleY), maximumScale);
		float centerX = ClientSize.Width / 2f;
		float centerY = ClientSize.Height / 2f;
		ScaleCanvas(scale, centerX, centerY);
		float contentCenterX = _CanvasValidBounds.Left + _CanvasValidBounds.Width / 2f;
		float contentCenterY = _CanvasValidBounds.Top + _CanvasValidBounds.Height / 2f;
		MoveCanvas(centerX - contentCenterX * _CanvasScale, centerY - contentCenterY * _CanvasScale, bAnimation: false, CanvasMoveArgs.All);
	}

	public ConnectionInfo[] GetConnectionInfo()
	{
		return GetConnections();
	}

	public ConnectionInfo[] GetConnections()
	{
		List<ConnectionInfo> connections = new List<ConnectionInfo>();
		foreach (STNode node in _Nodes)
		{
			foreach (STNodeOption output in node.GetAllOutputOptions())
			{
				foreach (STNodeOption input in output.ConnectedOption)
				{
					if (input != null && input.IsInput && input.Owner != null && input.Owner.Owner == this)
					{
						connections.Add(new ConnectionInfo { Output = output, Input = input });
					}
				}
			}
		}
		return connections
			.OrderBy(connection => _Nodes.IndexOf(connection.Output.Owner))
			.ThenBy(connection => Array.IndexOf(connection.Output.Owner.GetAllOutputOptions(), connection.Output))
			.ThenBy(connection => _Nodes.IndexOf(connection.Input.Owner))
			.ThenBy(connection => Array.IndexOf(connection.Input.Owner.GetAllInputOptions(), connection.Input))
			.ToArray();
	}

	public static bool CanFindNodePath(STNode nodeStart, STNode nodeFind)
	{
		HashSet<STNode> hs = new HashSet<STNode>();
		return CanFindNodePath(nodeStart, nodeFind, hs);
	}

	private static bool CanFindNodePath(STNode nodeStart, STNode nodeFind, HashSet<STNode> hs)
	{
		foreach (STNodeOption outputOption in nodeStart.OutputOptions)
		{
			if (outputOption.ConnectedOption == null)
			{
				continue;
			}
			foreach (STNodeOption item in outputOption.ConnectedOption)
			{
				if (item.Owner == nodeFind)
				{
					return true;
				}
				if (hs.Add(item.Owner) && CanFindNodePath(item.Owner, nodeFind))
				{
					return true;
				}
			}
		}
		return false;
	}

	public Image GetCanvasImage(Rectangle rect)
	{
		return GetCanvasImage(rect, 1f);
	}

	public Image GetCanvasImage(Rectangle rect, float fScale)
	{
		if ((double)fScale < 0.5)
		{
			fScale = 0.5f;
		}
		else if (fScale > 3f)
		{
			fScale = 3f;
		}
		int width = Math.Max(1, (int)Math.Ceiling(rect.Width * fScale));
		int height = Math.Max(1, (int)Math.Ceiling(rect.Height * fScale));
		Image image = new Bitmap(width, height);
		using (Graphics graphics = Graphics.FromImage(image))
		{
			graphics.Clear(BackColor);
			graphics.ScaleTransform(fScale, fScale);
			m_drawing_tools.Graphics = graphics;
			if (_ShowGrid)
			{
				OnDrawGrid(m_drawing_tools, rect.Width, rect.Height);
			}
			graphics.TranslateTransform(-rect.X, -rect.Y);
			OnDrawConnectedLine(m_drawing_tools);
			OnDrawNode(m_drawing_tools, rect);
			graphics.ResetTransform();
			if (_ShowLocation)
			{
				OnDrawNodeOutLocation(m_drawing_tools, image.Size, m_lst_node_out);
			}
		}
		return image;
	}

	public void SaveCanvas(string strFileName)
	{
		using FileStream s = new FileStream(strFileName, FileMode.Create, FileAccess.Write);
		SaveCanvas(s);
	}

	public void SaveCanvas(Stream s)
	{
		STNodeCanvasWriter.Write(
			s,
			_Nodes.Cast<STNode>(),
			GetConnections(),
			_CanvasOffsetX,
			_CanvasOffsetY,
			_CanvasScale);
	}

	public byte[] GetCanvasData()
	{
		using MemoryStream memoryStream = new MemoryStream();
		SaveCanvas(memoryStream);
		return memoryStream.ToArray();
	}

	public int LoadAssembly(string[] strFiles)
	{
		int num = 0;
		foreach (string strFile in strFiles)
		{
			try
			{
				if (LoadAssembly(strFile))
				{
					num++;
				}
			}
			catch
			{
			}
		}
		return num;
	}

	public int LoadAssembly()
	{
		return STNodeTypeRegistry.LoadAssemblies(AppDomain.CurrentDomain.GetAssemblies());
	}

	public bool LoadAssembly(string strFile)
	{
		return STNodeTypeRegistry.LoadAssembly(strFile);
	}

	public bool LoadAssembly(Assembly asm)
	{
		return STNodeTypeRegistry.LoadAssembly(asm);
	}

	public bool LoadAssemblyFromBase64(string base64Assembly)
	{
		byte[] rawAssembly = Convert.FromBase64String(base64Assembly);
		Assembly asm = Assembly.Load(rawAssembly);
		return LoadAssembly(asm);
	}

	public Type[] GetTypes()
	{
		return STNodeTypeRegistry.GetTypes();
	}

	public void LoadCanvas(string strFileName)
	{
		LoadCanvas(File.ReadAllBytes(strFileName));
	}

	public void LoadCanvas(byte[] byData)
	{
		using MemoryStream s = new MemoryStream(byData);
		LoadCanvas(s);
	}

	public void LoadCanvas(Stream s)
	{
		using (SuspendHistoryRecording())
		{
			LoadCanvasCore(s);
		}
	}

	private void LoadCanvasCore(Stream s)
	{
		STNodeCanvasReader.Document document = STNodeCanvasReader.Read(s);
		document.ConnectDetachedNodes();

		// Parsing and connection validation are complete before the live graph
		// is changed, so truncated/corrupt input cannot append a partial graph.
		// Loading intentionally preserves the upstream STND v1 append semantics.
		foreach (STNode node in document.Nodes)
			_Nodes.Add(node);
		ScaleCanvas(document.CanvasScale, 0f, 0f);
		MoveCanvas(
			document.CanvasOffsetX,
			document.CanvasOffsetY,
			bAnimation: false,
			CanvasMoveArgs.All);
		BuildBounds();
		foreach (STNode node in _Nodes)
		{
			node.OnEditorLoadCompleted();
		}
	}

	internal STNode GetNodeFromData(byte[] byData)
	{
		if (byData == null)
		{
			throw new ArgumentNullException(nameof(byData));
		}
		int offset = 0;
		string modelKey = ReadNodeByteLengthString(byData, ref offset, "节点类型");
		string typeKey = ReadNodeByteLengthString(byData, ref offset, "节点类型标识");
		Dictionary<string, byte[]> dictionary = new Dictionary<string, byte[]>();
		while (offset < byData.Length)
		{
			int keyLength = ReadNodeInt32(byData, ref offset, "属性名称长度");
			string propertyName = Encoding.UTF8.GetString(ReadNodeBytes(byData, ref offset, keyLength, "属性名称"));
			int valueLength = ReadNodeInt32(byData, ref offset, "属性值长度");
			byte[] propertyValue = ReadNodeBytes(byData, ref offset, valueLength, "属性值");
			if (dictionary.ContainsKey(propertyName))
			{
				throw new InvalidDataException($"节点数据包含重复属性：{propertyName}");
			}
			dictionary.Add(propertyName, propertyValue);
		}
		Type type = null;
		STNodeTypeRegistry.TryGetNodeType(typeKey, modelKey, out type);
		if (type == null)
		{
			throw new TypeLoadException($"无法找到节点类型 {{{modelKey}}}，请确认对应程序集已由编辑器加载");
		}
		STNode sTNode = (STNode)Activator.CreateInstance(type);
		sTNode.Create();
		sTNode.OnLoadNode(dictionary);
		return sTNode;
	}

	private static string ReadNodeByteLengthString(byte[] data, ref int offset, string valueName)
	{
		if (offset >= data.Length)
		{
			throw new InvalidDataException($"{valueName}缺失");
		}
		int length = data[offset++];
		return Encoding.UTF8.GetString(ReadNodeBytes(data, ref offset, length, valueName));
	}

	private static int ReadNodeInt32(byte[] data, ref int offset, string valueName)
	{
		byte[] value = ReadNodeBytes(data, ref offset, sizeof(int), valueName);
		return BitConverter.ToInt32(value, 0);
	}

	private static byte[] ReadNodeBytes(byte[] data, ref int offset, int length, string valueName)
	{
		if (length < 0 || offset < 0 || offset > data.Length - length)
		{
			throw new InvalidDataException($"{valueName}长度无效：{length}");
		}
		byte[] value = new byte[length];
		Buffer.BlockCopy(data, offset, value, 0, length);
		offset += length;
		return value;
	}

	public void ShowAlert(string strText, Color foreColor, Color backColor)
	{
		ShowAlert(strText, foreColor, backColor, 1000, AlertLocation.RightBottom, bRedraw: true);
	}

	public void ShowAlert(string strText, Color foreColor, Color backColor, AlertLocation al)
	{
		ShowAlert(strText, foreColor, backColor, 1000, al, bRedraw: true);
	}

	public void ShowAlert(string strText, Color foreColor, Color backColor, int nTime, AlertLocation al, bool bRedraw)
	{
		m_str_alert = strText;
		m_forecolor_alert = foreColor;
		m_backcolor_alert = backColor;
		m_time_alert = nTime;
		m_dt_alert = DateTime.UtcNow;
		m_alpha_alert = 255;
		m_al = al;
		if (bRedraw)
		{
			Invalidate();
		}
		UpdateAnimationTimerState();
	}

	public STNode SetActiveNode(STNode node)
	{
		if (node != null && !_Nodes.Contains(node))
		{
			return _ActiveNode;
		}
		STNode activeNode = _ActiveNode;
		if (_ActiveNode != node)
		{
			if (node != null)
			{
				_Nodes.MoveToEnd(node);
				node.IsActive = true;
				node.SetSelected(bSelected: true, bRedraw: false);
				node.OnGotFocus(EventArgs.Empty);
			}
			if (_ActiveNode != null)
			{
				_ActiveNode.IsActive = false;
				_ActiveNode.OnLostFocus(EventArgs.Empty);
			}
			_ActiveNode = node;
			Invalidate();
			OnActiveChanged(EventArgs.Empty);
		}
		return activeNode;
	}

	public bool AddSelectedNode(STNode node)
	{
		if (!_Nodes.Contains(node))
		{
			return false;
		}
		bool flag = !node.IsSelected;
		node.IsSelected = true;
		bool changed;
		lock (m_hs_node_selected)
		{
			changed = m_hs_node_selected.Add(node) || flag;
		}
		UpdateCanvasDragModeFromSelection();
		return changed;
	}

	public bool RemoveSelectedNode(STNode node)
	{
		if (!_Nodes.Contains(node))
		{
			return false;
		}
		bool isSelected = node.IsSelected;
		node.IsSelected = false;
		bool changed;
		lock (m_hs_node_selected)
		{
			changed = m_hs_node_selected.Remove(node) || isSelected;
		}
		UpdateCanvasDragModeFromSelection();
		return changed;
	}

	public Color SetTypeColor(Type t, Color clr)
	{
		return SetTypeColor(t, clr, bReplace: false);
	}

	public Color SetTypeColor(Type t, Color clr, bool bReplace)
	{
		if (_TypeColor.ContainsKey(t))
		{
			if (bReplace)
			{
				_TypeColor[t] = clr;
			}
		}
		else
		{
			_TypeColor.Add(t, clr);
		}
		return _TypeColor[t];
	}

	public void Dispose()
	{
		if (m_disposed)
		{
			return;
		}
		m_disposed = true;
		m_is_loaded = false;
		DisposeEditing();
		m_animation_timer.Stop();
		m_animation_timer.Tick -= AnimationTimer_Tick;
		ReleaseMouseCapture();
		foreach (GraphicsPath path in m_dic_gp_info.Keys)
		{
			path.Dispose();
		}
		m_dic_gp_info.Clear();
		m_gp_hover = null;
		m_p_line?.Dispose();
		m_p_line_hover?.Dispose();
		m_sf?.Dispose();
		m_img_border?.Dispose();
		m_img_border_hover?.Dispose();
		m_img_border_selected?.Dispose();
		m_img_border_active?.Dispose();
		m_drawing_tools.Pen?.Dispose();
		m_drawing_tools.SolidBrush?.Dispose();
		m_render_graphics?.Dispose();
		m_render_bitmap?.Dispose();
		m_measurement_bitmap.Dispose();
		_Font?.Dispose();
		GC.SuppressFinalize(this);
	}
}
