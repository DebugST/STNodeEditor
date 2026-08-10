using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Input;

namespace ST.Library.UI.NodeEditor;

public class STNodeControl
{
	private STNode _Owner;

	private int _Left;

	private int _Top;

	private int _Width;

	private int _Height;

	private Color _BackColor = Color.FromArgb(127, 0, 0, 0);

	private Color _ForeColor = Color.White;

	private string _Text = "STNCTRL";

	private Font _Font;

	private bool _Enabled = true;

	private bool _Visable = true;

	protected StringFormat m_sf;

	public STNode Owner
	{
		get
		{
			return _Owner;
		}
		internal set
		{
			_Owner = value;
		}
	}

	public int Left
	{
		get
		{
			return _Left;
		}
		set
		{
			_Left = value;
			OnMove(EventArgs.Empty);
			Invalidate();
		}
	}

	public int Top
	{
		get
		{
			return _Top;
		}
		set
		{
			_Top = value;
			OnMove(EventArgs.Empty);
			Invalidate();
		}
	}

	public int Width
	{
		get
		{
			return _Width;
		}
		set
		{
			_Width = value;
			OnResize(EventArgs.Empty);
			Invalidate();
		}
	}

	public int Height
	{
		get
		{
			return _Height;
		}
		set
		{
			_Height = value;
			OnResize(EventArgs.Empty);
			Invalidate();
		}
	}

	public int Right => _Left + _Width;

	public int Bottom => _Top + _Height;

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

	public Rectangle DisplayRectangle
	{
		get
		{
			return new Rectangle(_Left, _Top, _Width, _Height);
		}
		set
		{
			Left = value.X;
			Top = value.Y;
			Width = value.Width;
			Height = value.Height;
		}
	}

	public Rectangle ClientRectangle => new Rectangle(0, 0, _Width, _Height);

	public Color BackColor
	{
		get
		{
			return _BackColor;
		}
		set
		{
			_BackColor = value;
			Invalidate();
		}
	}

	public Color ForeColor
	{
		get
		{
			return _ForeColor;
		}
		set
		{
			_ForeColor = value;
			Invalidate();
		}
	}

	public string Text
	{
		get
		{
			return _Text;
		}
		set
		{
			_Text = value;
			Invalidate();
		}
	}

	public Font Font
	{
		get
		{
			return _Font;
		}
		set
		{
			if (value != _Font)
			{
				if (value == null)
				{
					throw new ArgumentNullException("值不能为空");
				}
				_Font = value;
				Invalidate();
			}
		}
	}

	public bool Enabled
	{
		get
		{
			return _Enabled;
		}
		set
		{
			if (value != _Enabled)
			{
				_Enabled = value;
				Invalidate();
			}
		}
	}

	public bool Visable
	{
		get
		{
			return _Visable;
		}
		set
		{
			if (value != _Visable)
			{
				_Visable = value;
				Invalidate();
			}
		}
	}

	public event EventHandler GotFocus;

	public event EventHandler LostFocus;

	public event EventHandler MouseEnter;

	public event EventHandler MouseLeave;

	public event STNodeMouseEventHandler MouseDown;

	public event STNodeMouseEventHandler MouseMove;

	public event STNodeMouseEventHandler MouseUp;

	public event STNodeMouseEventHandler MouseClick;

	public event STNodeMouseEventHandler MouseWheel;

	public event STNodeMouseEventHandler MouseHWheel;

	public event KeyEventHandler KeyDown;

	public event KeyEventHandler KeyUp;

	public event STNodeKeyPressEventHandler KeyPress;

	public event EventHandler Move;

	public event EventHandler Resize;

	public event STNodeControlPaintEventHandler Paint;

	public STNodeControl()
	{
		m_sf = new StringFormat();
		m_sf.Alignment = StringAlignment.Center;
		m_sf.LineAlignment = StringAlignment.Center;
		_Font = new Font("courier new", 8.25f);
		Width = 75;
		Height = 23;
	}

	protected internal virtual void OnPaint(DrawingTools dt)
	{
		Graphics graphics = dt.Graphics;
		SolidBrush solidBrush = dt.SolidBrush;
		graphics.SmoothingMode = SmoothingMode.None;
		graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
		solidBrush.Color = _BackColor;
		graphics.FillRectangle(solidBrush, 0, 0, Width, Height);
		if (!string.IsNullOrEmpty(_Text))
		{
			solidBrush.Color = _ForeColor;
			graphics.DrawString(_Text, _Font, solidBrush, ClientRectangle, m_sf);
		}
		if (this.Paint != null)
		{
			this.Paint(this, new STNodeControlPaintEventArgs(dt));
		}
	}

	public void Invalidate()
	{
		if (_Owner != null)
		{
			_Owner.Invalidate(new Rectangle(_Left, _Top + _Owner.TitleHeight, Width, Height));
		}
	}

	public void Invalidate(Rectangle rect)
	{
		if (_Owner != null)
		{
			_Owner.Invalidate(RectangleToParent(rect));
		}
	}

	public Rectangle RectangleToParent(Rectangle rect)
	{
		return new Rectangle(_Left, _Top + _Owner.TitleHeight, Width, Height);
	}

	protected internal virtual void OnGotFocus(EventArgs e)
	{
		if (this.GotFocus != null)
		{
			this.GotFocus(this, e);
		}
	}

	protected internal virtual void OnLostFocus(EventArgs e)
	{
		if (this.LostFocus != null)
		{
			this.LostFocus(this, e);
		}
	}

	protected internal virtual void OnMouseEnter(EventArgs e)
	{
		if (this.MouseEnter != null)
		{
			this.MouseEnter(this, e);
		}
	}

	protected internal virtual void OnMouseLeave(EventArgs e)
	{
		if (this.MouseLeave != null)
		{
			this.MouseLeave(this, e);
		}
	}

	protected internal virtual void OnMouseDown(STNodeMouseEventArgs e)
	{
		if (this.MouseDown != null)
		{
			this.MouseDown(this, e);
		}
	}

	protected internal virtual void OnMouseMove(STNodeMouseEventArgs e)
	{
		if (this.MouseMove != null)
		{
			this.MouseMove(this, e);
		}
	}

	protected internal virtual void OnMouseUp(STNodeMouseEventArgs e)
	{
		if (this.MouseUp != null)
		{
			this.MouseUp(this, e);
		}
	}

	protected internal virtual void OnMouseClick(STNodeMouseEventArgs e)
	{
		if (this.MouseClick != null)
		{
			this.MouseClick(this, e);
		}
	}

	protected internal virtual void OnMouseWheel(STNodeMouseEventArgs e)
	{
		if (this.MouseWheel != null)
		{
			this.MouseWheel(this, e);
		}
	}

	protected internal virtual void OnMouseHWheel(STNodeMouseEventArgs e)
	{
		if (this.MouseHWheel != null)
		{
			this.MouseHWheel(this, e);
		}
	}

	protected internal virtual void OnKeyDown(KeyEventArgs e)
	{
		if (this.KeyDown != null)
		{
			this.KeyDown(this, e);
		}
	}

	protected internal virtual void OnKeyUp(KeyEventArgs e)
	{
		if (this.KeyUp != null)
		{
			this.KeyUp(this, e);
		}
	}

	protected internal virtual void OnKeyPress(STNodeKeyPressEventArgs e)
	{
		if (this.KeyPress != null)
		{
			this.KeyPress(this, e);
		}
	}

	protected internal virtual void OnMove(EventArgs e)
	{
		if (this.Move != null)
		{
			this.Move(this, e);
		}
	}

	protected internal virtual void OnResize(EventArgs e)
	{
		if (this.Resize != null)
		{
			this.Resize(this, e);
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
}
