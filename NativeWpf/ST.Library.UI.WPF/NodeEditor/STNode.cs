using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Input;

namespace ST.Library.UI.NodeEditor;

public abstract class STNode : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler PropertyChanged;

	private STNodeEditor _Owner;

	private bool _IsSelected;

	private bool _IsActive;

	private Color _TitleColor;

	private Color _TitleProgressColor;

	private float _TitleProgress = -1f;

	private Color _MarkColor;

	private Color _ForeColor = Color.White;

	private Color _BackColor;

	private string _Title;

	private string _Mark;

	private string[] _MarkLines;

	private int _Left;

	private int _Top;

	private int _Width = 100;

	private int _Height = 40;

	private int _ItemHeight = 20;

	private bool _AutoSize = true;

	private Rectangle _MarkRectangle;

	private int _TitleHeight = 22;

	private STNodeOptionCollection _InputOptions;

	private STNodeOptionCollection _OutputOptions;

	private STNodeControlCollection _Controls;

	private Font _Font;

	private bool _ShowControls = true;

	private bool _LockOption;

	private bool _LockLocation;

	private object _Tag;

	private Guid _Guid;

	private bool _LetGetOptions;

	private int m_create_state;

	private static Point m_static_pt_init = new Point(10, 10);

	protected StringFormat m_sf;

	protected STNodeControl m_ctrl_active;

	protected STNodeControl m_ctrl_hover;

	protected STNodeControl m_ctrl_down;

	[Browsable(false)]
	public STNodeEditor Owner
	{
		get
		{
			return _Owner;
		}
		internal set
		{
			if (value == _Owner)
			{
				return;
			}
			if (_Owner != null)
			{
				STNodeOption[] array = _InputOptions.ToArray();
				foreach (STNodeOption sTNodeOption in array)
				{
					sTNodeOption.DisConnectionAll();
				}
				STNodeOption[] array2 = _OutputOptions.ToArray();
				foreach (STNodeOption sTNodeOption2 in array2)
				{
					sTNodeOption2.DisConnectionAll();
				}
			}
			_Owner = value;
			if (!_AutoSize)
			{
				SetOptionsLocation();
			}
			BuildSize(bBuildNode: true, bBuildMark: true, bRedraw: false);
			OnOwnerChanged();
		}
	}
    [Browsable(false)]
    public bool IsSelected
	{
		get
		{
			return _IsSelected;
		}
		set
		{
			if (value != _IsSelected)
			{
				_IsSelected = value;
				Invalidate();
				OnSelectedChanged();
				if (_Owner != null)
				{
					_Owner.OnSelectedChanged(EventArgs.Empty);
				}
			}
		}
	}
    [Browsable(false)]
    public bool IsActive
	{
		get
		{
			return _IsActive;
		}
		internal set
		{
			if (value != _IsActive)
			{
				_IsActive = value;
				OnActiveChanged();
			}
		}
	}

	public Color TitleColor
	{
		get
		{
			return _TitleColor;
		}
	    set
		{
			_TitleColor = value;
			Invalidate(new Rectangle(0, 0, _Width, _TitleHeight));
		}
	}

	[Browsable(false)]
	public Color TitleProgressColor
	{
		get
		{
			return _TitleProgressColor;
		}
		set
		{
			_TitleProgressColor = value;
			Invalidate(new Rectangle(0, 0, _Width, _TitleHeight));
		}
	}

	[Browsable(false)]
	public float TitleProgress
	{
		get
		{
			return _TitleProgress;
		}
		set
		{
			float progress = float.IsNaN(value) || value < 0f ? -1f : (value > 1f ? 1f : value);
			if (Math.Abs(_TitleProgress - progress) < 0.0001f)
			{
				return;
			}
			_TitleProgress = progress;
			Invalidate(new Rectangle(0, 0, _Width, _TitleHeight));
		}
	}

	public Color MarkColor
	{
		get
		{
			return _MarkColor;
		}
	  set
		{
			_MarkColor = value;
			Invalidate(_MarkRectangle);
		}
	}

	public Color ForeColor
	{
		get
		{
			return _ForeColor;
		}
		protected set
		{
			_ForeColor = value;
			Invalidate();
		}
	}

	public Color BackColor
	{
		get
		{
			return _BackColor;
		}
		protected set
		{
			_BackColor = value;
			Invalidate();
		}
	}

    [STNodeProperty("Title", "Title", true)]
    [Browsable(true)]
    public string Title
	{
		get
		{
			return _Title;
		}
		set
		{
			_Title = value;
			if (_AutoSize)
			{
				BuildSize(bBuildNode: true, bBuildMark: true, bRedraw: true);
			}
			OnPropertyChanged();
		}
	}

	protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public string Mark
	{
		get
		{
			return _Mark;
		}
		set
		{
			_Mark = value;
			if (value == null)
			{
				_MarkLines = null;
			}
			else
			{
				_MarkLines = (from s in value.Split('\n')
					select s.Trim()).ToArray();
			}
			Invalidate(new Rectangle(-5, -5, _MarkRectangle.Width + 10, _MarkRectangle.Height + 10));
		}
	}

	public string[] MarkLines => _MarkLines;
    [Browsable(false)]
    public int Left
	{
		get
		{
			return _Left;
		}
		set
		{
			if (!_LockLocation && value != _Left)
			{
				Point oldLocation = new Point(_Left, _Top);
				_Left = value;
				SetOptionsLocation();
				BuildSize(bBuildNode: false, bBuildMark: true, bRedraw: false);
				OnMove(EventArgs.Empty);
				if (_Owner != null)
				{
					_Owner.BuildLinePath();
					_Owner.BuildBounds();
					_Owner.OnNodeLocationChanged(this, oldLocation, new Point(_Left, _Top));
				}
			}
		}
	}
    [Browsable(false)]
    public int Top
	{
		get
		{
			return _Top;
		}
		set
		{
			if (!_LockLocation && value != _Top)
			{
				Point oldLocation = new Point(_Left, _Top);
				_Top = value;
				SetOptionsLocation();
				BuildSize(bBuildNode: false, bBuildMark: true, bRedraw: false);
				OnMove(EventArgs.Empty);
				if (_Owner != null)
				{
					_Owner.BuildLinePath();
					_Owner.BuildBounds();
					_Owner.OnNodeLocationChanged(this, oldLocation, new Point(_Left, _Top));
				}
			}
		}
	}

	public int Width
	{
		get
		{
			return _Width;
		}
		protected set
		{
			if (value >= 50 && !_AutoSize && value != _Width)
			{
				_Width = value;
				SetOptionsLocation();
				BuildSize(bBuildNode: false, bBuildMark: true, bRedraw: false);
				OnResize(EventArgs.Empty);
				if (_Owner != null)
				{
					_Owner.BuildLinePath();
					_Owner.BuildBounds();
				}
				Invalidate();
			}
		}
	}

	public int Height
	{
		get
		{
			return _Height;
		}
		protected set
		{
			if (value >= 40 && !_AutoSize && value != _Height)
			{
				_Height = value;
				SetOptionsLocation();
				BuildSize(bBuildNode: false, bBuildMark: true, bRedraw: false);
				OnResize(EventArgs.Empty);
				if (_Owner != null)
				{
					_Owner.BuildLinePath();
					_Owner.BuildBounds();
				}
				Invalidate();
			}
		}
	}

	public int ItemHeight
	{
		get
		{
			return _ItemHeight;
		}
		protected set
		{
			if (value < 16)
			{
				value = 16;
			}
			if (value > 200)
			{
				value = 200;
			}
			if (value == _ItemHeight)
			{
				return;
			}
			_ItemHeight = value;
			if (_AutoSize)
			{
				BuildSize(bBuildNode: true, bBuildMark: false, bRedraw: true);
				return;
			}
			SetOptionsLocation();
			if (_Owner != null)
			{
				_Owner.Invalidate();
			}
		}
	}

	public bool AutoSize
	{
		get
		{
			return _AutoSize;
		}
		protected set
		{
			_AutoSize = value;
		}
	}

	public int Right => _Left + _Width;

	public int Bottom => _Top + _Height;

	public Rectangle Rectangle => new Rectangle(_Left, _Top, _Width, _Height);

	public Rectangle TitleRectangle => new Rectangle(_Left, _Top, _Width, _TitleHeight);

	public Rectangle MarkRectangle => _MarkRectangle;

	public int TitleHeight
	{
		get
		{
			return _TitleHeight;
		}
		protected set
		{
			_TitleHeight = value;
		}
	}

	protected internal STNodeOptionCollection InputOptions => _InputOptions;

	public int InputOptionsCount => _InputOptions.Count;

	protected internal STNodeOptionCollection OutputOptions => _OutputOptions;

	public int OutputOptionsCount => _OutputOptions.Count;

	protected STNodeControlCollection Controls => _Controls;

	public int ControlsCount => _Controls.Count;

	protected bool ShowControls
	{
		get
		{
			return _ShowControls;
		}
		set
		{
			if (_ShowControls == value)
			{
				return;
			}
			_ShowControls = value;
			if (!_ShowControls)
			{
				m_ctrl_active = null;
				m_ctrl_hover = null;
				m_ctrl_down = null;
			}
			Invalidate();
		}
	}

	public Point Location
	{
		get
		{
			return new Point(_Left, _Top);
		}
		set
		{
			Left = value.X;
			Top = value.Y;
		}
	}

	public Size Size
	{
		get
		{
			return new Size(_Width, _Height);
		}
		set
		{
			Width = value.Width;
			Height = value.Height;
		}
	}

	public void SetFixedWidth(int width)
	{
		SetFixedSize(width, _Height);
	}

	public void SetAutoSize(bool autoSize)
	{
		if (_AutoSize == autoSize)
		{
			if (_AutoSize)
			{
				BuildSize(bBuildNode: true, bBuildMark: true, bRedraw: false);
			}
			return;
		}
		_AutoSize = autoSize;
		if (_AutoSize)
		{
			BuildSize(bBuildNode: true, bBuildMark: true, bRedraw: false);
		}
		else
		{
			SetOptionsLocation();
			BuildSize(bBuildNode: false, bBuildMark: true, bRedraw: false);
			OnResize(EventArgs.Empty);
		}
		if (_Owner != null)
		{
			_Owner.BuildLinePath();
			_Owner.BuildBounds();
		}
		Invalidate();
	}

	public void SetFixedSize(int width, int height)
	{
		if (width < 50)
		{
			width = 50;
		}
		if (height < 40)
		{
			height = 40;
		}
		if (!_AutoSize && width == _Width && height == _Height)
		{
			return;
		}
		_AutoSize = false;
		_Width = width;
		_Height = height;
		SetOptionsLocation();
		BuildSize(bBuildNode: false, bBuildMark: true, bRedraw: false);
		OnResize(EventArgs.Empty);
		if (_Owner != null)
		{
			_Owner.BuildLinePath();
			_Owner.BuildBounds();
		}
		Invalidate();
	}

	protected Font Font
	{
		get
		{
			return _Font;
		}
		set
		{
			if (value != _Font)
			{
				_Font.Dispose();
				_Font = value;
			}
		}
	}

    [Browsable(false)]
    public bool LockOption
	{
		get
		{
			return _LockOption;
		}
		set
		{
			_LockOption = value;
			Invalidate(new Rectangle(0, 0, _Width, _TitleHeight));
		}
	}
    [Browsable(false)]
    public bool LockLocation
	{
		get
		{
			return _LockLocation;
		}
		set
		{
			_LockLocation = value;
			Invalidate(new Rectangle(0, 0, _Width, _TitleHeight));
		}
	}

	public object Tag
	{
		get
		{
			return _Tag;
		}
		set
		{
			_Tag = value;
		}
	}

	public Guid Guid => _Guid;

	internal void RegenerateGuid()
	{
		Guid oldGuid = _Guid;
		_Guid = Guid.NewGuid();
		OnGuidRegenerated(oldGuid, _Guid);
	}

	protected virtual void OnGuidRegenerated(Guid oldGuid, Guid newGuid)
	{
	}

	public bool LetGetOptions
	{
		get
		{
			return _LetGetOptions;
		}
		protected set
		{
			_LetGetOptions = value;
		}
	}

	/// <summary>
	/// Gets whether the node creation lifecycle has completed.
	/// </summary>
	[Browsable(false)]
	public bool IsCreated => Volatile.Read(ref m_create_state) == 2;

	public STNode()
	{
		_Title = "Untitled";
		_MarkRectangle.Height = _Height;
		_Left = (_MarkRectangle.X = m_static_pt_init.X);
		_Top = m_static_pt_init.Y;
		_MarkRectangle.Y = _Top - 30;
		_InputOptions = new STNodeOptionCollection(this, isInput: true);
		_OutputOptions = new STNodeOptionCollection(this, isInput: false);
		_Controls = new STNodeControlCollection(this);
		_BackColor = Color.FromArgb(200, 64, 64, 64);
		_TitleColor = Color.FromArgb(200, Color.DodgerBlue);
		_TitleProgressColor = Color.FromArgb(230, Color.DeepSkyBlue);
		_MarkColor = Color.FromArgb(200, Color.Brown);
		_Font = new Font("courier new", 8.25f);
		m_sf = new StringFormat();
		m_sf.Alignment = StringAlignment.Near;
		m_sf.LineAlignment = StringAlignment.Center;
		m_sf.FormatFlags = StringFormatFlags.NoWrap;
		m_sf.SetTabStops(0f, new float[1] { 40f });
		m_static_pt_init.X += 10;
		m_static_pt_init.Y += 10;
		_Guid = Guid.NewGuid();
		Create();
	}

	protected internal void BuildSize(bool bBuildNode, bool bBuildMark, bool bRedraw)
	{
		STNodeEditor owner = _Owner;
		if (owner == null || owner.IsDisposed)
		{
			return;
		}
		if (!owner.Dispatcher.CheckAccess())
		{
			// Runtime nodes update option captions from thread-pool callbacks.
			// WPF layout and its shared measurement bitmap belong to the editor
			// dispatcher, so defer only the visual work and let execution continue.
			owner.BeginInvoke(new Action(() =>
			{
				if (ReferenceEquals(_Owner, owner))
				{
					BuildSize(bBuildNode, bBuildMark, bRedraw);
				}
			}));
			return;
		}
		if (_AutoSize && bBuildNode)
		{
			Size defaultNodeSize = GetDefaultNodeSize();
			if (_Width != defaultNodeSize.Width || _Height != defaultNodeSize.Height)
			{
				_Width = defaultNodeSize.Width;
				_Height = defaultNodeSize.Height;
				SetOptionsLocation();
				OnResize(EventArgs.Empty);
			}
		}
		if (bBuildMark && !string.IsNullOrEmpty(_Mark))
		{
			_MarkRectangle = OnBuildMarkRectangle();
		}
		if (bRedraw)
		{
			owner.Invalidate();
		}
	}

	internal Dictionary<string, byte[]> OnSaveNode()
	{
		Dictionary<string, byte[]> dictionary = new Dictionary<string, byte[]>();
		dictionary.Add("Guid", _Guid.ToByteArray());
		dictionary.Add("Left", BitConverter.GetBytes(_Left));
		dictionary.Add("Top", BitConverter.GetBytes(_Top));
		dictionary.Add("Width", BitConverter.GetBytes(_Width));
		dictionary.Add("Height", BitConverter.GetBytes(_Height));
		dictionary.Add("AutoSize", new byte[1] { _AutoSize ? ((byte)1) : ((byte)0) });
		if (_Mark != null)
		{
			dictionary.Add("Mark", Encoding.UTF8.GetBytes(_Mark));
		}
		dictionary.Add("LockOption", new byte[1] { _LockOption ? ((byte)1) : ((byte)0) });
		dictionary.Add("LockLocation", new byte[1] { _LockLocation ? ((byte)1) : ((byte)0) });
		Type type = GetType();
		PropertyInfo[] properties = type.GetProperties();
		foreach (PropertyInfo propertyInfo in properties)
		{
			object[] customAttributes = propertyInfo.GetCustomAttributes(inherit: true);
			object[] array = customAttributes;
			foreach (object obj in array)
			{
				if (obj is STNodePropertyAttribute)
				{
					STNodePropertyAttribute sTNodePropertyAttribute = obj as STNodePropertyAttribute;
					object obj2 = Activator.CreateInstance(sTNodePropertyAttribute.DescriptorType);
					if (!(obj2 is STNodePropertyDescriptor))
					{
						throw new InvalidOperationException("[STNodePropertyAttribute.Type]参数值必须为[STNodePropertyDescriptor]或者其子类的类型");
					}
					STNodePropertyDescriptor sTNodePropertyDescriptor = (STNodePropertyDescriptor)Activator.CreateInstance(sTNodePropertyAttribute.DescriptorType);
					sTNodePropertyDescriptor.Node = this;
					sTNodePropertyDescriptor.PropertyInfo = propertyInfo;
					byte[] bytesFromValue = sTNodePropertyDescriptor.GetBytesFromValue();
					if (bytesFromValue != null)
					{
						dictionary.Add(propertyInfo.Name, bytesFromValue);
					}
				}
			}
		}
		OnSaveNode(dictionary);
		return dictionary;
	}

	public byte[] GetSaveData()
	{
		List<byte> list = new List<byte>();
		Type type = GetType();
		byte[] bytes = Encoding.UTF8.GetBytes(STNodeTypeRegistry.GetModelByType(type));
		list.Add((byte)bytes.Length);
		list.AddRange(bytes);
		bytes = Encoding.UTF8.GetBytes(type.GUID.ToString());
		list.Add((byte)bytes.Length);
		list.AddRange(bytes);
		Dictionary<string, byte[]> dictionary = OnSaveNode();
		if (dictionary != null)
		{
			foreach (KeyValuePair<string, byte[]> item in dictionary)
			{
				bytes = Encoding.UTF8.GetBytes(item.Key);
				list.AddRange(BitConverter.GetBytes(bytes.Length));
				list.AddRange(bytes);
				list.AddRange(BitConverter.GetBytes(item.Value.Length));
				list.AddRange(item.Value);
			}
		}
		return list.ToArray();
	}

	protected virtual void OnCreate()
	{
	}

	protected virtual void OnCreated()
	{
	}

	protected internal virtual void OnDrawNode(DrawingTools dt)
	{
		Graphics graphics = dt.Graphics;
		int cornerRadius = _Owner?.NodeCornerRadius ?? 0;
		if (_BackColor.A != 0)
		{
			dt.SolidBrush.Color = _BackColor;
			if (cornerRadius > 0)
			{
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using GraphicsPath nodePath = CreateRoundedRectanglePath(Rectangle, cornerRadius);
				GraphicsState graphicsState = graphics.Save();
				graphics.SetClip(nodePath, CombineMode.Intersect);
				graphics.FillRectangle(dt.SolidBrush, _Left, _Top + _TitleHeight, _Width, Height - _TitleHeight);
				graphics.Restore(graphicsState);
			}
			else
			{
				graphics.SmoothingMode = SmoothingMode.None;
				graphics.FillRectangle(dt.SolidBrush, _Left, _Top + _TitleHeight, _Width, Height - _TitleHeight);
			}
		}
		OnDrawTitle(dt);
		OnDrawBody(dt);
	}

	public virtual string OnGetDrawTitle()
	{
		return _Title;
	}

	protected virtual void OnDrawTitle(DrawingTools dt)
	{
		m_sf.Alignment = StringAlignment.Center;
		m_sf.LineAlignment = StringAlignment.Center;
		Graphics graphics = dt.Graphics;
		SolidBrush solidBrush = dt.SolidBrush;
		if (_TitleColor.A != 0)
		{
			solidBrush.Color = _TitleColor;
			int cornerRadius = _Owner?.NodeCornerRadius ?? 0;
			if (cornerRadius > 0)
			{
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				using GraphicsPath nodePath = CreateRoundedRectanglePath(Rectangle, cornerRadius);
				GraphicsState graphicsState = graphics.Save();
				graphics.SetClip(nodePath, CombineMode.Intersect);
				graphics.FillRectangle(solidBrush, TitleRectangle);
				graphics.Restore(graphicsState);
			}
			else
			{
				graphics.FillRectangle(solidBrush, TitleRectangle);
			}
		}
		if (_TitleProgress > 0f && _TitleProgressColor.A != 0)
		{
			Rectangle progressRectangle = TitleRectangle;
			progressRectangle.Width = (int)Math.Round(progressRectangle.Width * _TitleProgress);
			if (progressRectangle.Width > 0)
			{
				solidBrush.Color = _TitleProgressColor;
				int cornerRadius = _Owner?.NodeCornerRadius ?? 0;
				if (cornerRadius > 0)
				{
					graphics.SmoothingMode = SmoothingMode.AntiAlias;
					using GraphicsPath nodePath = CreateRoundedRectanglePath(Rectangle, cornerRadius);
					GraphicsState graphicsState = graphics.Save();
					graphics.SetClip(nodePath, CombineMode.Intersect);
					graphics.FillRectangle(solidBrush, progressRectangle);
					graphics.Restore(graphicsState);
				}
				else
				{
					graphics.FillRectangle(solidBrush, progressRectangle);
				}
			}
		}
		if (_LockOption)
		{
			solidBrush.Color = _ForeColor;
			int num = _Top + _TitleHeight / 2 - 5;
			graphics.FillRectangle(dt.SolidBrush, _Left + 4, num, 2, 4);
			graphics.FillRectangle(dt.SolidBrush, _Left + 6, num, 2, 2);
			graphics.FillRectangle(dt.SolidBrush, _Left + 8, num, 2, 4);
			graphics.FillRectangle(dt.SolidBrush, _Left + 3, num + 4, 8, 6);
		}
		if (_LockLocation)
		{
			solidBrush.Color = _ForeColor;
			int num2 = _Top + _TitleHeight / 2 - 5;
			graphics.FillRectangle(solidBrush, Right - 9, num2, 4, 4);
			graphics.FillRectangle(solidBrush, Right - 11, num2 + 4, 8, 2);
			graphics.FillRectangle(solidBrush, Right - 8, num2 + 6, 2, 4);
		}
		string text = OnGetDrawTitle();
		if (!string.IsNullOrEmpty(text) && _ForeColor.A != 0)
		{
			solidBrush.Color = _ForeColor;
			graphics.SmoothingMode = SmoothingMode.HighQuality;
			graphics.DrawString(text, _Font, solidBrush, GetTitleTextRectangle(), m_sf);
		}
	}

	protected virtual Rectangle GetTitleTextRectangle()
	{
		Rectangle rectangle = TitleRectangle;
		rectangle.Offset(0, 2);
		return rectangle;
	}

	private static GraphicsPath CreateRoundedRectanglePath(Rectangle rectangle, int radius)
	{
		GraphicsPath path = new GraphicsPath();
		int maxRadius = Math.Min(rectangle.Width, rectangle.Height) / 2;
		radius = Math.Min(radius, maxRadius);
		if (radius <= 0)
		{
			path.AddRectangle(rectangle);
			return path;
		}

		int diameter = radius * 2;
		path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180f, 90f);
		path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270f, 90f);
		path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0f, 90f);
		path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90f, 90f);
		path.CloseFigure();
		return path;
	}

	protected virtual void OnDrawBody(DrawingTools dt)
	{
		SolidBrush solidBrush = dt.SolidBrush;
		foreach (STNodeOption inputOption in _InputOptions)
		{
			if (inputOption != STNodeOption.Empty)
			{
				OnDrawOptionDot(dt, inputOption);
				OnDrawOptionText(dt, inputOption);
			}
		}
		foreach (STNodeOption outputOption in _OutputOptions)
		{
			if (outputOption != STNodeOption.Empty)
			{
				OnDrawOptionDot(dt, outputOption);
				OnDrawOptionText(dt, outputOption);
			}
		}
		if (!_ShowControls || _Controls.Count == 0)
		{
			return;
		}
		dt.Graphics.TranslateTransform(_Left, _Top + _TitleHeight);
		Point empty = Point.Empty;
		Point point = Point.Empty;
		foreach (STNodeControl control in _Controls)
		{
			if (control.Visable)
			{
				empty.X = control.Left - point.X;
				empty.Y = control.Top - point.Y;
				point = control.Location;
				dt.Graphics.TranslateTransform(empty.X, empty.Y);
				dt.Graphics.SmoothingMode = SmoothingMode.None;
				control.OnPaint(dt);
			}
		}
		dt.Graphics.TranslateTransform(-_Left - point.X, -_Top - _TitleHeight - point.Y);
	}

	protected internal virtual void OnDrawMark(DrawingTools dt)
	{
		if (!string.IsNullOrEmpty(_Mark))
		{
			Graphics graphics = dt.Graphics;
			SolidBrush solidBrush = dt.SolidBrush;
			m_sf.LineAlignment = StringAlignment.Center;
			graphics.SmoothingMode = SmoothingMode.None;
			solidBrush.Color = _MarkColor;
			graphics.FillRectangle(solidBrush, _MarkRectangle);
			graphics.SmoothingMode = SmoothingMode.HighQuality;
			SizeF sizeF = graphics.MeasureString(Mark, Font, _MarkRectangle.Width);
			solidBrush.Color = _ForeColor;
			if (sizeF.Height > (float)_ItemHeight || sizeF.Width > (float)_MarkRectangle.Width)
			{
				Rectangle rectangle = new Rectangle(_MarkRectangle.Left + 2, _MarkRectangle.Top + 2, _MarkRectangle.Width - 20, 16);
				m_sf.Alignment = StringAlignment.Near;
				graphics.DrawString(_MarkLines[0], _Font, solidBrush, rectangle, m_sf);
				m_sf.Alignment = StringAlignment.Far;
				rectangle.Width = _MarkRectangle.Width - 5;
				graphics.DrawString("+", _Font, solidBrush, rectangle, m_sf);
			}
			else
			{
				m_sf.Alignment = StringAlignment.Near;
				graphics.DrawString(_MarkLines[0].Trim(), _Font, solidBrush, _MarkRectangle, m_sf);
			}
		}
	}

	protected virtual void OnDrawOptionDot(DrawingTools dt, STNodeOption op)
	{
		Graphics graphics = dt.Graphics;
		Pen pen = dt.Pen;
		SolidBrush solidBrush = dt.SolidBrush;
		Type typeFromHandle = typeof(object);
		if (op == null || Owner == null)
		{
			return;
		}
		if (op.DotColor != Color.Transparent)
		{
			solidBrush.Color = op.DotColor;
		}
		else if (op.DataType == typeFromHandle)
		{
			pen.Color = Owner.UnknownTypeColor;
		}
		else
		{
			solidBrush.Color = (Owner.TypeColor.ContainsKey(op.DataType) ? Owner.TypeColor[op.DataType] : Owner.UnknownTypeColor);
		}
		if (op.IsSingle)
		{
			graphics.SmoothingMode = SmoothingMode.HighQuality;
			if (op.DataType == typeFromHandle)
			{
				graphics.DrawEllipse(pen, op.DotRectangle.X, op.DotRectangle.Y, op.DotRectangle.Width - 1, op.DotRectangle.Height - 1);
			}
			else
			{
				graphics.FillEllipse(solidBrush, op.DotRectangle);
			}
		}
		else
		{
			graphics.SmoothingMode = SmoothingMode.None;
			if (op.DataType == typeFromHandle)
			{
				graphics.DrawRectangle(pen, op.DotRectangle.X, op.DotRectangle.Y, op.DotRectangle.Width - 1, op.DotRectangle.Height - 1);
			}
			else
			{
				graphics.FillRectangle(solidBrush, op.DotRectangle);
			}
		}
	}

	protected virtual void OnDrawOptionText(DrawingTools dt, STNodeOption op)
	{
		Graphics graphics = dt.Graphics;
		SolidBrush solidBrush = dt.SolidBrush;
		if (op.IsInput)
		{
			m_sf.Alignment = StringAlignment.Near;
		}
		else
		{
			m_sf.Alignment = StringAlignment.Far;
		}
		solidBrush.Color = op.TextColor;
		graphics.DrawString(op.Text, Font, solidBrush, op.TextRectangle, m_sf);
	}

	protected virtual Point OnSetOptionDotLocation(STNodeOption op, Point pt, int nIndex)
	{
		return pt;
	}

	protected virtual Rectangle OnSetOptionTextRectangle(STNodeOption op, Rectangle rect, int nIndex)
	{
		return rect;
	}

	protected virtual Size GetDefaultNodeSize(Graphics g)
	{
		int num = 0;
		int num2 = 0;
		foreach (STNodeOption inputOption in _InputOptions)
		{
			num += _ItemHeight;
		}
		foreach (STNodeOption outputOption in _OutputOptions)
		{
			num2 += _ItemHeight;
		}
		int height = _TitleHeight + ((num > num2) ? num : num2);
		SizeF sizeF = SizeF.Empty;
		SizeF sizeF2 = SizeF.Empty;
		foreach (STNodeOption inputOption2 in _InputOptions)
		{
			if (!string.IsNullOrEmpty(inputOption2.Text))
			{
				SizeF sizeF3 = g.MeasureString(inputOption2.Text, _Font);
				if (sizeF3.Width > sizeF.Width)
				{
					sizeF = sizeF3;
				}
			}
		}
		foreach (STNodeOption outputOption2 in _OutputOptions)
		{
			if (!string.IsNullOrEmpty(outputOption2.Text))
			{
				SizeF sizeF4 = g.MeasureString(outputOption2.Text, _Font);
				if (sizeF4.Width > sizeF2.Width)
				{
					sizeF2 = sizeF4;
				}
			}
		}
		int num3 = (int)(sizeF.Width + sizeF2.Width + 25f);
		if (!string.IsNullOrEmpty(Title))
		{
			sizeF = g.MeasureString(Title, Font);
		}
		if (sizeF.Width + 30f > (float)num3)
		{
			num3 = (int)sizeF.Width + 30;
		}
		return new Size(num3, height);
	}

	protected virtual Size GetDefaultNodeSize()
	{
		if (_Owner != null)
		{
			using Graphics graphics = _Owner.CreateGraphics();
			return GetDefaultNodeSize(graphics);
		}
		using Bitmap bitmap = new Bitmap(1, 1);
		using Graphics fallbackGraphics = Graphics.FromImage(bitmap);
		return GetDefaultNodeSize(fallbackGraphics);
	}

	protected virtual Rectangle OnBuildMarkRectangle(Graphics g)
	{
		return new Rectangle(_Left, _Top - 30, _Width, 20);
	}

	protected virtual Rectangle OnBuildMarkRectangle()
	{
		if (_Owner != null)
		{
			using Graphics graphics = _Owner.CreateGraphics();
			return OnBuildMarkRectangle(graphics);
		}
		using Bitmap bitmap = new Bitmap(1, 1);
		using Graphics fallbackGraphics = Graphics.FromImage(bitmap);
		return OnBuildMarkRectangle(fallbackGraphics);
	}

	protected virtual void OnSaveNode(Dictionary<string, byte[]> dic)
	{
	}

	protected internal virtual void OnLoadNode(Dictionary<string, byte[]> dic)
	{
		if (dic.ContainsKey("AutoSize"))
		{
			_AutoSize = dic["AutoSize"][0] == 1;
		}
		if (dic.ContainsKey("LockOption"))
		{
			_LockOption = dic["LockOption"][0] == 1;
		}
		if (dic.ContainsKey("LockLocation"))
		{
			_LockLocation = dic["LockLocation"][0] == 1;
		}
		if (dic.ContainsKey("Guid"))
		{
			_Guid = new Guid(dic["Guid"]);
		}
		if (dic.ContainsKey("Left"))
		{
			_Left = BitConverter.ToInt32(dic["Left"], 0);
		}
		if (dic.ContainsKey("Top"))
		{
			_Top = BitConverter.ToInt32(dic["Top"], 0);
		}
		if (dic.ContainsKey("Width") && !_AutoSize)
		{
			_Width = BitConverter.ToInt32(dic["Width"], 0);
		}
		if (dic.ContainsKey("Height") && !_AutoSize)
		{
			_Height = BitConverter.ToInt32(dic["Height"], 0);
		}
		if (dic.ContainsKey("Mark"))
		{
			Mark = Encoding.UTF8.GetString(dic["Mark"]);
		}
		Type type = GetType();
		PropertyInfo[] properties = type.GetProperties();
		foreach (PropertyInfo propertyInfo in properties)
		{
			object[] customAttributes = propertyInfo.GetCustomAttributes(inherit: true);
			object[] array = customAttributes;
			foreach (object obj in array)
			{
				if (!(obj is STNodePropertyAttribute))
				{
					continue;
				}
				STNodePropertyAttribute sTNodePropertyAttribute = obj as STNodePropertyAttribute;
				object obj2 = Activator.CreateInstance(sTNodePropertyAttribute.DescriptorType);
				if (!(obj2 is STNodePropertyDescriptor))
				{
					throw new InvalidOperationException("[STNodePropertyAttribute.Type]参数值必须为[STNodePropertyDescriptor]或者其子类的类型");
				}
				STNodePropertyDescriptor sTNodePropertyDescriptor = (STNodePropertyDescriptor)Activator.CreateInstance(sTNodePropertyAttribute.DescriptorType);
				sTNodePropertyDescriptor.Node = this;
				sTNodePropertyDescriptor.PropertyInfo = propertyInfo;
				try
				{
					if (dic.ContainsKey(propertyInfo.Name))
					{
						sTNodePropertyDescriptor.SetValue(dic[propertyInfo.Name]);
					}
				}
				catch (Exception ex)
				{
					string text = "属性[" + Title + "." + propertyInfo.Name + "]的值无法被还原 可通过重写[STNodePropertyAttribute.GetBytesFromValue(),STNodePropertyAttribute.GetValueFromBytes(byte[])]确保保存和加载时候的二进制数据正确";
					for (Exception ex2 = ex; ex2 != null; ex2 = ex2.InnerException)
					{
						text = text + "\r\n----\r\n[" + ex2.GetType().Name + "] -> " + ex2.Message;
					}
					throw new InvalidOperationException(text, ex);
				}
			}
		}
		SetOptionsLocation();
	}

	protected internal virtual void OnEditorLoadCompleted()
	{
	}

	protected bool SetOptionText(STNodeOption op, string strText)
	{
		if (op.Owner != this)
		{
			return false;
		}
		op.Text = strText;
		return true;
	}

	protected bool SetOptionTextColor(STNodeOption op, Color clr)
	{
		if (op.Owner != this)
		{
			return false;
		}
		op.TextColor = clr;
		return true;
	}

	protected bool SetOptionDotColor(STNodeOption op, Color clr)
	{
		if (op.Owner != this)
		{
			return false;
		}
		op.DotColor = clr;
		return false;
	}

	protected internal virtual void OnGotFocus(EventArgs e)
	{
	}

	protected internal virtual void OnLostFocus(EventArgs e)
	{
	}

	protected internal virtual void OnMouseEnter(EventArgs e)
	{
	}

	protected internal virtual void OnMouseDown(STNodeMouseEventArgs e)
	{
		if (!_ShowControls)
		{
			if (m_ctrl_active != null)
			{
				m_ctrl_active.OnLostFocus(EventArgs.Empty);
			}
			m_ctrl_active = null;
			return;
		}
		Point location = e.Location;
		location.Y -= _TitleHeight;
		for (int num = _Controls.Count - 1; num >= 0; num--)
		{
			STNodeControl sTNodeControl = _Controls[num];
			if (sTNodeControl.DisplayRectangle.Contains(location))
			{
				if (!sTNodeControl.Enabled)
				{
					return;
				}
				if (sTNodeControl.Visable)
				{
					sTNodeControl.OnMouseDown(e.WithLocation(e.X - sTNodeControl.Left, location.Y - sTNodeControl.Top));
					m_ctrl_down = sTNodeControl;
					if (m_ctrl_active != sTNodeControl)
					{
						sTNodeControl.OnGotFocus(EventArgs.Empty);
						if (m_ctrl_active != null)
						{
							m_ctrl_active.OnLostFocus(EventArgs.Empty);
						}
						m_ctrl_active = sTNodeControl;
					}
					return;
				}
			}
		}
		if (m_ctrl_active != null)
		{
			m_ctrl_active.OnLostFocus(EventArgs.Empty);
		}
		m_ctrl_active = null;
	}

	protected internal virtual void OnMouseMove(STNodeMouseEventArgs e)
	{
		if (!_ShowControls)
		{
			m_ctrl_down = null;
			if (m_ctrl_hover != null)
			{
				m_ctrl_hover.OnMouseLeave(EventArgs.Empty);
			}
			m_ctrl_hover = null;
			return;
		}
		Point location = e.Location;
		location.Y -= _TitleHeight;
		if (m_ctrl_down != null)
		{
			if (m_ctrl_down.Enabled && m_ctrl_down.Visable)
			{
				m_ctrl_down.OnMouseMove(e.WithLocation(e.X - m_ctrl_down.Left, location.Y - m_ctrl_down.Top));
			}
			return;
		}
		for (int num = _Controls.Count - 1; num >= 0; num--)
		{
			STNodeControl sTNodeControl = _Controls[num];
			if (sTNodeControl.DisplayRectangle.Contains(location))
			{
				if (m_ctrl_hover != _Controls[num])
				{
					sTNodeControl.OnMouseEnter(EventArgs.Empty);
					if (m_ctrl_hover != null)
					{
						m_ctrl_hover.OnMouseLeave(EventArgs.Empty);
					}
					m_ctrl_hover = sTNodeControl;
				}
				m_ctrl_hover.OnMouseMove(e.WithLocation(e.X - sTNodeControl.Left, location.Y - sTNodeControl.Top));
				return;
			}
		}
		if (m_ctrl_hover != null)
		{
			m_ctrl_hover.OnMouseLeave(EventArgs.Empty);
		}
		m_ctrl_hover = null;
	}

	protected internal virtual void OnMouseUp(STNodeMouseEventArgs e)
	{
		if (!_ShowControls)
		{
			m_ctrl_down = null;
			return;
		}
		Point location = e.Location;
		location.Y -= _TitleHeight;
		if (m_ctrl_down != null && m_ctrl_down.Enabled && m_ctrl_down.Visable)
		{
			m_ctrl_down.OnMouseUp(e.WithLocation(e.X - m_ctrl_down.Left, location.Y - m_ctrl_down.Top));
		}
		m_ctrl_down = null;
	}

	protected internal virtual void CancelMouseInteraction()
	{
		m_ctrl_down = null;
	}

	protected internal virtual void OnMouseLeave(EventArgs e)
	{
		if (m_ctrl_hover != null && m_ctrl_hover.Enabled && m_ctrl_hover.Visable)
		{
			m_ctrl_hover.OnMouseLeave(e);
		}
		m_ctrl_hover = null;
	}

	protected internal virtual void OnMouseClick(STNodeMouseEventArgs e)
	{
		if (!_ShowControls)
		{
			return;
		}
		Point location = e.Location;
		location.Y -= _TitleHeight;
		if (m_ctrl_active != null && m_ctrl_active.Enabled && m_ctrl_active.Visable)
		{
			m_ctrl_active.OnMouseClick(e.WithLocation(e.X - m_ctrl_active.Left, location.Y - m_ctrl_active.Top));
		}
	}

	protected internal virtual void OnMouseWheel(STNodeMouseEventArgs e)
	{
		if (!_ShowControls)
		{
			return;
		}
		Point location = e.Location;
		location.Y -= _TitleHeight;
		if (m_ctrl_hover != null && m_ctrl_hover.Enabled && m_ctrl_hover.Visable)
		{
			m_ctrl_hover.OnMouseWheel(e.WithLocation(e.X - m_ctrl_hover.Left, location.Y - m_ctrl_hover.Top));
		}
	}

	protected internal virtual void OnMouseHWheel(STNodeMouseEventArgs e)
	{
		if (m_ctrl_hover != null && m_ctrl_hover.Enabled && m_ctrl_hover.Visable)
		{
			m_ctrl_hover.OnMouseHWheel(e);
		}
	}

	protected internal virtual void OnKeyDown(KeyEventArgs e)
	{
		if (m_ctrl_active != null && m_ctrl_active.Enabled && m_ctrl_active.Visable)
		{
			m_ctrl_active.OnKeyDown(e);
		}
	}

	protected internal virtual void OnKeyUp(KeyEventArgs e)
	{
		if (m_ctrl_active != null && m_ctrl_active.Enabled && m_ctrl_active.Visable)
		{
			m_ctrl_active.OnKeyUp(e);
		}
	}

	protected internal virtual void OnKeyPress(STNodeKeyPressEventArgs e)
	{
		if (m_ctrl_active != null && m_ctrl_active.Enabled && m_ctrl_active.Visable)
		{
			m_ctrl_active.OnKeyPress(e);
		}
	}

	protected virtual void OnMove(EventArgs e)
	{
	}

	protected virtual void OnResize(EventArgs e)
	{
	}

	protected virtual void OnOwnerChanged()
	{
	}

	protected virtual void OnSelectedChanged()
	{
	}

	protected virtual void OnActiveChanged()
	{
	}

	protected virtual void SetOptionsLocation()
	{
		int num = 0;
		Rectangle rect = new Rectangle(Left + 10, _Top + _TitleHeight, _Width - 20, _ItemHeight);
		foreach (STNodeOption inputOption in _InputOptions)
		{
			if (inputOption != STNodeOption.Empty)
			{
				Point point = OnSetOptionDotLocation(inputOption, new Point(Left - inputOption.DotSize / 2, rect.Y + (rect.Height - inputOption.DotSize) / 2), num);
				inputOption.TextRectangle = OnSetOptionTextRectangle(inputOption, rect, num);
				inputOption.DotLeft = point.X;
				inputOption.DotTop = point.Y;
			}
			rect.Y += _ItemHeight;
			num++;
		}
		rect.Y = _Top + _TitleHeight;
		m_sf.Alignment = StringAlignment.Far;
		foreach (STNodeOption outputOption in _OutputOptions)
		{
			if (outputOption != STNodeOption.Empty)
			{
				Point point2 = OnSetOptionDotLocation(outputOption, new Point(_Left + _Width - outputOption.DotSize / 2, rect.Y + (rect.Height - outputOption.DotSize) / 2), num);
				outputOption.TextRectangle = OnSetOptionTextRectangle(outputOption, rect, num);
				outputOption.DotLeft = point2.X;
				outputOption.DotTop = point2.Y;
			}
			rect.Y += _ItemHeight;
			num++;
		}
	}

	public void Invalidate()
	{
		if (_Owner != null)
		{
			_Owner.Invalidate(_Owner.CanvasToControl(new Rectangle(_Left - 5, _Top - 5, _Width + 10, _Height + 10)));
		}
	}

	public void Invalidate(Rectangle rect)
	{
		rect.X += _Left;
		rect.Y += _Top;
		if (_Owner != null)
		{
			rect = _Owner.CanvasToControl(rect);
			rect.Width++;
			rect.Height++;
			_Owner.Invalidate(rect);
		}
	}

	public STNodeOption[] GetInputOptions()
	{
		if (!_LetGetOptions)
		{
			return null;
		}
		STNodeOption[] array = new STNodeOption[_InputOptions.Count];
		for (int i = 0; i < _InputOptions.Count; i++)
		{
			array[i] = _InputOptions[i];
		}
		return array;
	}

	public STNodeOption[] GetOutputOptions()
	{
		if (!_LetGetOptions)
		{
			return null;
		}
		STNodeOption[] array = new STNodeOption[_OutputOptions.Count];
		for (int i = 0; i < _OutputOptions.Count; i++)
		{
			array[i] = _OutputOptions[i];
		}
		return array;
	}

	/// <summary>
	/// Returns all input options regardless of LetGetOptions setting.
	/// Used for serialization/copy-paste operations.
	/// </summary>
	public STNodeOption[] GetAllInputOptions()
	{
		STNodeOption[] array = new STNodeOption[_InputOptions.Count];
		for (int i = 0; i < _InputOptions.Count; i++)
		{
			array[i] = _InputOptions[i];
		}
		return array;
	}

	public bool ReorderInputOptions(IReadOnlyList<STNodeOption> orderedOptions)
	{
		if (!_InputOptions.Reorder(orderedOptions))
		{
			return false;
		}

		SetOptionsLocation();
		BuildSize(bBuildNode: true, bBuildMark: true, bRedraw: false);
		_Owner?.BuildLinePath();
		Invalidate();
		return true;
	}

	/// <summary>
	/// Returns all output options regardless of LetGetOptions setting.
	/// Used for serialization/copy-paste operations.
	/// </summary>
	public STNodeOption[] GetAllOutputOptions()
	{
		STNodeOption[] array = new STNodeOption[_OutputOptions.Count];
		for (int i = 0; i < _OutputOptions.Count; i++)
		{
			array[i] = _OutputOptions[i];
		}
		return array;
	}

	public void SetSelected(bool bSelected, bool bRedraw)
	{
		if (_IsSelected == bSelected)
		{
			return;
		}
		_IsSelected = bSelected;
		if (_Owner != null)
		{
			if (bSelected)
			{
				_Owner.AddSelectedNode(this);
			}
			else
			{
				_Owner.RemoveSelectedNode(this);
			}
		}
		if (bRedraw)
		{
			Invalidate();
		}
		OnSelectedChanged();
		if (_Owner != null)
		{
			_Owner.OnSelectedChanged(EventArgs.Empty);
		}
	}

	public IAsyncResult BeginInvoke(Delegate method)
	{
		return BeginInvoke(method, null);
	}

	public IAsyncResult BeginInvoke(Delegate method, params object[] args)
	{
		if (_Owner == null)
		{
			return null;
		}
		return _Owner.BeginInvoke(method, args);
	}

	public object Invoke(Delegate method)
	{
		return Invoke(method, null);
	}

	public object Invoke(Delegate method, params object[] args)
	{
		if (_Owner == null)
		{
			return null;
		}
		return _Owner.Invoke(method, args);
	}

	public void Create()
	{
		if (Interlocked.CompareExchange(ref m_create_state, 1, 0) != 0)
		{
			return;
		}

		try
		{
			OnCreate();
			OnCreated();
			Volatile.Write(ref m_create_state, 2);
		}
		catch
		{
			Volatile.Write(ref m_create_state, 0);
			throw;
		}
	}
}
