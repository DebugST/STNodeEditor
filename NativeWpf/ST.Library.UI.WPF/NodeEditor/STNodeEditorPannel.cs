using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using DrawingColor = System.Drawing.Color;
using DrawingSize = System.Drawing.Size;
using MediaColor = System.Windows.Media.Color;

namespace ST.Library.UI.NodeEditor;

/// <summary>
/// WPF composite editor containing the node catalog, canvas and property editor.
/// The historical type name is retained for source compatibility.
/// </summary>
public class STNodeEditorPannel : UserControl, IDisposable
{
	private readonly Grid m_root = new Grid();
	private readonly STNodeEditor m_editor = new STNodeEditor();
	private readonly STNodeTreeView m_tree = new STNodeTreeView();
	private readonly STNodePropertyGrid m_grid = new STNodePropertyGrid();
	private readonly Dictionary<ConnectionStatus, string> m_status_text = new Dictionary<ConnectionStatus, string>();

	private bool m_left_layout = true;
	private DrawingColor m_split_line_color = DrawingColor.Black;
	private DrawingColor m_handle_line_color = DrawingColor.Gray;
	private DrawingColor m_back_color = DrawingColor.FromArgb(255, 34, 34, 34);
	private bool m_show_scale = true;
	private bool m_show_connection_status = true;
	private int m_x = 201;
	private int m_y = 250;
	private bool m_disposed;
	private GridSplitter m_vertical_splitter;
	private GridSplitter m_horizontal_splitter;
	private Grid m_side_grid;

	[DefaultValue(true)]
	public bool LeftLayout
	{
		get => m_left_layout;
		set
		{
			if (m_left_layout == value)
			{
				return;
			}
			m_left_layout = value;
			double width = GetViewportWidth();
			m_x = value ? 201 : Math.Max(122, (int)width - 202);
			BuildLayout();
		}
	}

	[DefaultValue(typeof(DrawingColor), "Black")]
	public DrawingColor SplitLineColor
	{
		get => m_split_line_color;
		set
		{
			m_split_line_color = value;
			ApplySplitterColors();
		}
	}

	[DefaultValue(typeof(DrawingColor), "Gray")]
	public DrawingColor HandleLineColor
	{
		get => m_handle_line_color;
		set
		{
			m_handle_line_color = value;
			ApplySplitterColors();
		}
	}

	[DefaultValue(true)]
	public bool ShowScale
	{
		get => m_show_scale;
		set => m_show_scale = value;
	}

	[DefaultValue(true)]
	public bool ShowConnectionStatus
	{
		get => m_show_connection_status;
		set => m_show_connection_status = value;
	}

	[DefaultValue(201)]
	public int X
	{
		get => m_x;
		set
		{
			m_x = Clamp(value, 122, Math.Max(122, (int)GetViewportWidth() - 122));
			BuildLayout();
		}
	}

	public int Y
	{
		get => m_y;
		set
		{
			m_y = Clamp(value, 122, Math.Max(122, (int)GetViewportHeight() - 122));
			BuildLayout();
		}
	}

	[Browsable(false)]
	public STNodeEditor Editor => m_editor;

	[Browsable(false)]
	public STNodeTreeView TreeView => m_tree;

	[Browsable(false)]
	public STNodePropertyGrid PropertyGrid => m_grid;

	public DrawingColor BackColor
	{
		get => m_back_color;
		set
		{
			m_back_color = value;
			Background = ToBrush(value);
		}
	}

	public DrawingSize MinimumSize
	{
		get => new DrawingSize((int)MinWidth, (int)MinHeight);
		set
		{
			MinWidth = Math.Max(250, value.Width);
			MinHeight = Math.Max(250, value.Height);
		}
	}

	public STNodeEditorPannel()
	{
		Width = 500;
		Height = 500;
		MinWidth = 250;
		MinHeight = 250;
		Content = m_root;
		Background = ToBrush(m_back_color);
		m_grid.Text = "NodeProperty";

		foreach (ConnectionStatus status in Enum.GetValues(typeof(ConnectionStatus)))
		{
			FieldInfo field = typeof(ConnectionStatus).GetField(status.ToString());
			string text = field?.GetCustomAttributes(typeof(DescriptionAttribute), inherit: true)
				.OfType<DescriptionAttribute>()
				.FirstOrDefault()?.Description ?? status.ToString();
			m_status_text[status] = text;
		}

		m_editor.ActiveChanged += OnEditorActiveChanged;
		m_editor.CanvasScaled += OnEditorCanvasScaled;
		m_editor.OptionConnected += OnEditorOptionConnected;
		SizeChanged += OnPanelSizeChanged;
		BuildLayout();
	}

	public bool AddSTNode(Type nodeType)
	{
		return m_tree.AddNode(nodeType);
	}

	public int LoadAssembly()
	{
		m_editor.LoadAssembly();
		return m_tree.LoadAssembly();
	}

	public int LoadAssembly(string fileName)
	{
		m_editor.LoadAssembly(fileName);
		return m_tree.LoadAssembly(fileName);
	}

	public string SetConnectionStatusText(ConnectionStatus status, string text)
	{
		if (m_status_text.TryGetValue(status, out string previous))
		{
			m_status_text[status] = text;
			return previous;
		}
		m_status_text.Add(status, text);
		return text;
	}

	private void BuildLayout()
	{
		if (m_root == null)
		{
			return;
		}

		// Removing the side grid from the root does not detach its children from
		// their logical parent. Detach the reusable controls before rebuilding it.
		if (m_side_grid != null)
		{
			m_side_grid.Children.Remove(m_tree);
			m_side_grid.Children.Remove(m_grid);
		}

		m_root.Children.Clear();
		m_root.ColumnDefinitions.Clear();
		m_root.RowDefinitions.Clear();

		double width = GetViewportWidth();
		double sideWidth = m_left_layout
			? Clamp(m_x, 122, Math.Max(122, (int)width - 122))
			: Clamp((int)width - m_x, 122, Math.Max(122, (int)width - 122));

		m_root.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = m_left_layout ? new GridLength(sideWidth) : new GridLength(1, GridUnitType.Star)
		});
		m_root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
		m_root.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = m_left_layout ? new GridLength(1, GridUnitType.Star) : new GridLength(sideWidth)
		});

		m_side_grid = new Grid();
		double height = GetViewportHeight();
		double topHeight = Clamp(m_y, 122, Math.Max(122, (int)height - 122));
		m_side_grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(topHeight) });
		m_side_grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5) });
		m_side_grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		m_side_grid.Children.Add(m_tree);
		Grid.SetRow(m_grid, 2);
		m_side_grid.Children.Add(m_grid);
		m_horizontal_splitter = new GridSplitter
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			ResizeDirection = GridResizeDirection.Rows,
			ResizeBehavior = GridResizeBehavior.PreviousAndNext,
			Cursor = System.Windows.Input.Cursors.SizeNS
		};
		m_horizontal_splitter.DragCompleted += OnHorizontalSplitterDragCompleted;
		Grid.SetRow(m_horizontal_splitter, 1);
		m_side_grid.Children.Add(m_horizontal_splitter);

		int sideColumn = m_left_layout ? 0 : 2;
		Grid.SetColumn(m_side_grid, sideColumn);
		m_root.Children.Add(m_side_grid);

		int editorColumn = m_left_layout ? 2 : 0;
		Grid.SetColumn(m_editor, editorColumn);
		m_root.Children.Add(m_editor);

		m_vertical_splitter = new GridSplitter
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			ResizeDirection = GridResizeDirection.Columns,
			ResizeBehavior = GridResizeBehavior.PreviousAndNext,
			Cursor = System.Windows.Input.Cursors.SizeWE
		};
		m_vertical_splitter.DragCompleted += OnVerticalSplitterDragCompleted;
		Grid.SetColumn(m_vertical_splitter, 1);
		m_root.Children.Add(m_vertical_splitter);
		ApplySplitterColors();
	}

	private void OnPanelSizeChanged(object sender, SizeChangedEventArgs e)
	{
		double width = GetViewportWidth();
		m_x = Clamp(m_x, 122, Math.Max(122, (int)width - 122));
		m_y = Clamp(m_y, 122, Math.Max(122, (int)GetViewportHeight() - 122));
	}

	private void OnVerticalSplitterDragCompleted(object sender, DragCompletedEventArgs e)
	{
		if (m_root.ColumnDefinitions.Count < 3)
		{
			return;
		}
		m_x = m_left_layout
			? (int)Math.Round(m_root.ColumnDefinitions[0].ActualWidth)
			: (int)Math.Round(GetViewportWidth() - m_root.ColumnDefinitions[2].ActualWidth);
	}

	private void OnHorizontalSplitterDragCompleted(object sender, DragCompletedEventArgs e)
	{
		if (m_side_grid?.RowDefinitions.Count >= 3)
		{
			m_y = (int)Math.Round(m_side_grid.RowDefinitions[0].ActualHeight);
		}
	}

	private void OnEditorActiveChanged(object sender, EventArgs e)
	{
		m_grid.SetNode(m_editor.ActiveNode);
	}

	private void OnEditorCanvasScaled(object sender, EventArgs e)
	{
		if (m_show_scale)
		{
			m_editor.ShowAlert(
				m_editor.CanvasScale.ToString("F2"),
				DrawingColor.White,
				DrawingColor.FromArgb(127, 255, 255, 0));
		}
	}

	private void OnEditorOptionConnected(object sender, STNodeEditorOptionEventArgs e)
	{
		if (!m_show_connection_status)
		{
			return;
		}
		string text = m_status_text.TryGetValue(e.Status, out string value) ? value : e.Status.ToString();
		m_editor.ShowAlert(
			text,
			DrawingColor.White,
			e.Status == ConnectionStatus.Connected
				? DrawingColor.FromArgb(125, DrawingColor.Lime)
				: DrawingColor.FromArgb(125, DrawingColor.Red));
	}

	private void ApplySplitterColors()
	{
		if (m_vertical_splitter != null)
		{
			m_vertical_splitter.Background = ToBrush(m_split_line_color);
			m_vertical_splitter.BorderBrush = ToBrush(m_handle_line_color);
		}
		if (m_horizontal_splitter != null)
		{
			m_horizontal_splitter.Background = ToBrush(m_split_line_color);
			m_horizontal_splitter.BorderBrush = ToBrush(m_handle_line_color);
		}
	}

	private double GetViewportWidth()
	{
		if (ActualWidth > 0)
		{
			return ActualWidth;
		}
		return double.IsNaN(Width) || Width <= 0 ? 500 : Width;
	}

	private double GetViewportHeight()
	{
		if (ActualHeight > 0)
		{
			return ActualHeight;
		}
		return double.IsNaN(Height) || Height <= 0 ? 500 : Height;
	}

	private static int Clamp(int value, int minimum, int maximum)
	{
		if (maximum < minimum)
		{
			maximum = minimum;
		}
		return Math.Max(minimum, Math.Min(value, maximum));
	}

	private static System.Windows.Media.SolidColorBrush ToBrush(DrawingColor color)
	{
		var brush = new System.Windows.Media.SolidColorBrush(MediaColor.FromArgb(color.A, color.R, color.G, color.B));
		brush.Freeze();
		return brush;
	}

	public void Dispose()
	{
		if (m_disposed)
		{
			return;
		}
		m_disposed = true;
		SizeChanged -= OnPanelSizeChanged;
		m_editor.ActiveChanged -= OnEditorActiveChanged;
		m_editor.CanvasScaled -= OnEditorCanvasScaled;
		m_editor.OptionConnected -= OnEditorOptionConnected;
		if (m_vertical_splitter != null)
		{
			m_vertical_splitter.DragCompleted -= OnVerticalSplitterDragCompleted;
		}
		if (m_horizontal_splitter != null)
		{
			m_horizontal_splitter.DragCompleted -= OnHorizontalSplitterDragCompleted;
		}
		m_editor.Dispose();
		m_tree.Dispose();
		m_grid.Dispose();
		GC.SuppressFinalize(this);
	}
}
