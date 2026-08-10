using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using DrawingColor = System.Drawing.Color;
using DrawingFont = System.Drawing.Font;
using DrawingSize = System.Drawing.Size;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using MediaFontFamily = System.Windows.Media.FontFamily;
using WpfPoint = System.Windows.Point;

namespace ST.Library.UI.NodeEditor;

/// <summary>
/// WPF node catalog with search, drag-and-drop creation and node preview.
/// </summary>
public class STNodeTreeView : UserControl, IDisposable
{
	private sealed class CatalogNode
	{
		public string Name { get; set; }

		public Type NodeType { get; set; }

		public DrawingColor NodeColor { get; set; } = DrawingColor.DarkCyan;

		public List<CatalogNode> Children { get; } = new List<CatalogNode>();

		public int NodeCount => NodeType == null
			? Children.Sum(child => child.NodeCount)
			: 1;
	}

	private readonly Dictionary<Type, string> m_dic_all_type = new Dictionary<Type, string>();
	private readonly Dictionary<Type, string> m_dic_node_title = new Dictionary<Type, string>();
	private readonly Dictionary<Type, DrawingColor> m_dic_node_color = new Dictionary<Type, DrawingColor>();
	private readonly STNodeEditor _editor;
	private readonly STNodePropertyGrid _property_grid;
	private readonly TextBox m_search_box;
	private readonly Button m_clear_button;
	private readonly System.Windows.Controls.TreeView m_tree;
	private readonly Popup m_preview_popup;

	private string m_search_text = string.Empty;
	private WpfPoint m_drag_start;
	private Type m_drag_type;
	private bool m_disposed;
	private DrawingFont m_font = new DrawingFont("Segoe UI", 9f);

	private DrawingColor m_item_back_color = DrawingColor.FromArgb(255, 45, 45, 45);
	private DrawingColor m_item_hover_color = DrawingColor.FromArgb(50, 125, 125, 125);
	private DrawingColor m_title_color = DrawingColor.FromArgb(255, 60, 60, 60);
	private DrawingColor m_text_box_color = DrawingColor.FromArgb(255, 30, 30, 30);
	private DrawingColor m_highlight_text_color = DrawingColor.Lime;
	private DrawingColor m_info_button_color = DrawingColor.Gray;
	private DrawingColor m_folder_count_color = DrawingColor.FromArgb(100, 255, 255, 255);
	private DrawingColor m_back_color = DrawingColor.FromArgb(255, 35, 35, 35);
	private DrawingColor m_fore_color = DrawingColor.FromArgb(255, 220, 220, 220);
	private bool m_show_folder_count = true;
	private bool m_show_info_button = true;
	private bool m_info_panel_is_left_layout = true;
	private bool m_auto_color = true;

	public DrawingColor ItemBackColor
	{
		get => m_item_back_color;
		set
		{
			m_item_back_color = value;
			ApplyColors();
		}
	}

	public DrawingColor ItemHoverColor
	{
		get => m_item_hover_color;
		set => m_item_hover_color = value;
	}

	public DrawingColor TitleColor
	{
		get => m_title_color;
		set
		{
			m_title_color = value;
			ApplyColors();
		}
	}

	public DrawingColor TextBoxColor
	{
		get => m_text_box_color;
		set
		{
			m_text_box_color = value;
			ApplyColors();
		}
	}

	public DrawingColor HightLightTextColor
	{
		get => m_highlight_text_color;
		set
		{
			m_highlight_text_color = value;
			RefreshTree();
		}
	}

	public DrawingColor InfoButtonColor
	{
		get => m_info_button_color;
		set
		{
			m_info_button_color = value;
			RefreshTree();
		}
	}

	public DrawingColor FolderCountColor
	{
		get => m_folder_count_color;
		set
		{
			m_folder_count_color = value;
			RefreshTree();
		}
	}

	public DrawingColor BackColor
	{
		get => m_back_color;
		set
		{
			m_back_color = value;
			ApplyColors();
		}
	}

	public DrawingColor ForeColor
	{
		get => m_fore_color;
		set
		{
			m_fore_color = value;
			ApplyColors();
		}
	}

	public DrawingFont Font
	{
		get => m_font;
		set
		{
			if (value == null || ReferenceEquals(value, m_font))
			{
				return;
			}
			m_font.Dispose();
			m_font = value;
			ApplyFont();
		}
	}

	[DefaultValue(true)]
	public bool ShowFolderCount
	{
		get => m_show_folder_count;
		set
		{
			m_show_folder_count = value;
			RefreshTree();
		}
	}

	[DefaultValue(true)]
	public bool ShowInfoButton
	{
		get => m_show_info_button;
		set
		{
			m_show_info_button = value;
			RefreshTree();
		}
	}

	[DefaultValue(true)]
	public bool InfoPanelIsLeftLayout
	{
		get => m_info_panel_is_left_layout;
		set => m_info_panel_is_left_layout = value;
	}

	[DefaultValue(true)]
	public bool AutoColor
	{
		get => m_auto_color;
		set
		{
			m_auto_color = value;
			RefreshTree();
		}
	}

	[Browsable(false)]
	public STNodeEditor Editor => _editor;

	[Browsable(false)]
	public STNodePropertyGrid PropertyGrid => _property_grid;

	[Browsable(false)]
	public IReadOnlyDictionary<Type, string> NodeTypes => m_dic_all_type;

	public STNodeTreeView()
	{
		MinWidth = 100;
		MinHeight = 60;

		var root = new DockPanel();
		Content = root;

		var search_border = new Border
		{
			Padding = new Thickness(5)
		};
		var search_grid = new Grid();
		search_grid.ColumnDefinitions.Add(new ColumnDefinition());
		search_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		m_search_box = new TextBox
		{
			MaxLength = 50,
			BorderThickness = new Thickness(0),
			Padding = new Thickness(5, 2, 5, 2),
			VerticalContentAlignment = VerticalAlignment.Center
		};
		m_search_box.TextChanged += OnSearchTextChanged;
		search_grid.Children.Add(m_search_box);
		m_clear_button = new Button
		{
			Content = "×",
			MinWidth = 24,
			Padding = new Thickness(3, 0, 3, 0),
			Margin = new Thickness(4, 0, 0, 0),
			Visibility = Visibility.Collapsed,
			ToolTip = "清除搜索"
		};
		m_clear_button.Click += OnClearSearchClick;
		Grid.SetColumn(m_clear_button, 1);
		search_grid.Children.Add(m_clear_button);
		search_border.Child = search_grid;
		DockPanel.SetDock(search_border, Dock.Top);
		root.Children.Add(search_border);

		m_tree = new System.Windows.Controls.TreeView
		{
			BorderThickness = new Thickness(0),
			Padding = new Thickness(2)
		};
		m_tree.PreviewMouseLeftButtonDown += OnTreeMouseLeftButtonDown;
		m_tree.PreviewMouseMove += OnTreeMouseMove;
		root.Children.Add(m_tree);

		_editor = new STNodeEditor
		{
			LimitCanvasToContentBounds = false,
			ShowLocation = false,
			ShowBorder = false,
			ClientSize = new DrawingSize(360, 280)
		};
		_property_grid = new STNodePropertyGrid
		{
			Width = 260
		};
		var preview_grid = new Grid
		{
			Width = 620,
			Height = 300,
			Background = ToBrush(m_back_color)
		};
		preview_grid.ColumnDefinitions.Add(new ColumnDefinition());
		preview_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
		preview_grid.Children.Add(_editor);
		Grid.SetColumn(_property_grid, 1);
		preview_grid.Children.Add(_property_grid);
		m_preview_popup = new Popup
		{
			AllowsTransparency = true,
			StaysOpen = false,
			Child = new Border
			{
				BorderBrush = MediaBrushes.DimGray,
				BorderThickness = new Thickness(1),
				Background = ToBrush(m_back_color),
				Child = preview_grid
			}
		};

		ApplyColors();
		ApplyFont();
		LoadAssembly();
	}

	public void Search(string text)
	{
		m_search_box.Text = text?.Trim() ?? string.Empty;
	}

	public Type[] GetVisibleTypes()
	{
		return m_dic_all_type
			.Where(entry => MatchesSearch(entry.Key, entry.Value))
			.Select(entry => entry.Key)
			.ToArray();
	}

	public bool AddNode(Type nodeType)
	{
		if (nodeType == null)
		{
			return false;
		}
		if (!nodeType.IsSubclassOf(typeof(STNode)))
		{
			throw new ArgumentException($"不支持的类型[{nodeType.FullName}] [nodeType]参数值必须为[STNode]子类的类型", nameof(nodeType));
		}
		if (nodeType.IsAbstract || nodeType.IsDefined(typeof(ObsoleteAttribute), inherit: false) || m_dic_all_type.ContainsKey(nodeType))
		{
			return false;
		}

		var attribute = nodeType.GetCustomAttributes(typeof(STNodeAttribute), inherit: true)
			.OfType<STNodeAttribute>()
			.FirstOrDefault();
		if (attribute == null)
		{
			throw new InvalidOperationException($"类型[{nodeType.FullName}]未被[STNodeAttribute]所标记");
		}

		string path = attribute.Path?.Trim('/', '\\') ?? string.Empty;
		CacheNodeMetadata(nodeType);
		m_dic_all_type.Add(nodeType, path);
		RefreshTree();
		return true;
	}

	public int LoadAssembly()
	{
		int count = 0;
		foreach (Assembly assembly in STNodeTypeRegistry.GetAssemblies())
		{
			count += AddAssembly(assembly);
		}
		if (count > 0)
		{
			RefreshTree();
		}
		return count;
	}

	public int LoadAssembly(string fileName)
	{
		Assembly assembly = Assembly.LoadFrom(Path.GetFullPath(fileName));
		STNodeTypeRegistry.LoadAssembly(assembly);
		int count = AddAssembly(assembly);
		if (count > 0)
		{
			RefreshTree();
		}
		return count;
	}

	public void Clear()
	{
		m_dic_all_type.Clear();
		m_dic_node_title.Clear();
		m_dic_node_color.Clear();
		RefreshTree();
	}

	public bool RemoveNode(Type nodeType)
	{
		bool removed = m_dic_all_type.Remove(nodeType);
		if (removed)
		{
			m_dic_node_title.Remove(nodeType);
			m_dic_node_color.Remove(nodeType);
			RefreshTree();
		}
		return removed;
	}

	private int AddAssembly(Assembly assembly)
	{
		int count = 0;
		foreach (Type nodeType in STNodeTypeRegistry.GetTypes(assembly))
		{
			try
			{
				if (AddNodeWithoutRefresh(nodeType))
				{
					count++;
				}
			}
			catch
			{
			}
		}
		return count;
	}

	private bool AddNodeWithoutRefresh(Type nodeType)
	{
		if (nodeType == null
			|| nodeType.IsAbstract
			|| !nodeType.IsSubclassOf(typeof(STNode))
			|| nodeType.IsDefined(typeof(ObsoleteAttribute), inherit: false)
			|| m_dic_all_type.ContainsKey(nodeType))
		{
			return false;
		}

		var attribute = nodeType.GetCustomAttributes(typeof(STNodeAttribute), inherit: true)
			.OfType<STNodeAttribute>()
			.FirstOrDefault();
		if (attribute == null)
		{
			return false;
		}
		string assemblyName = nodeType.Assembly.GetName().Name ?? "Unknown";
		string path = string.IsNullOrWhiteSpace(attribute.Path)
			? assemblyName
			: $"{assemblyName}/{attribute.Path.Trim('/', '\\')}";
		CacheNodeMetadata(nodeType);
		m_dic_all_type.Add(nodeType, path);
		return true;
	}

	private void CacheNodeMetadata(Type nodeType)
	{
		if (Activator.CreateInstance(nodeType) is not STNode node)
		{
			throw new InvalidOperationException($"无法创建节点类型[{nodeType.FullName}]");
		}

		m_dic_node_title[nodeType] = string.IsNullOrWhiteSpace(node.Title) ? nodeType.Name : node.Title;
		m_dic_node_color[nodeType] = node.TitleColor;
	}

	private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
	{
		m_search_text = m_search_box.Text.Trim();
		m_clear_button.Visibility = m_search_text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
		RefreshTree();
	}

	private void OnClearSearchClick(object sender, RoutedEventArgs e)
	{
		m_search_box.Clear();
		m_search_box.Focus();
	}

	private void RefreshTree()
	{
		if (m_tree == null)
		{
			return;
		}

		m_tree.Items.Clear();
		foreach (CatalogNode node in BuildCatalog())
		{
			m_tree.Items.Add(CreateTreeItem(node));
		}
	}

	private List<CatalogNode> BuildCatalog()
	{
		var roots = new List<CatalogNode>();
		foreach (KeyValuePair<Type, string> entry in m_dic_all_type.Where(item => MatchesSearch(item.Key, item.Value)))
		{
			List<CatalogNode> level = roots;
			foreach (string segment in entry.Value.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries))
			{
				CatalogNode folder = level.FirstOrDefault(item => item.NodeType == null && item.Name == segment);
				if (folder == null)
				{
					folder = new CatalogNode { Name = segment };
					level.Add(folder);
				}
				level = folder.Children;
			}

			string title = m_dic_node_title.TryGetValue(entry.Key, out string cachedTitle)
				? cachedTitle
				: entry.Key.Name;
			DrawingColor nodeColor = m_dic_node_color.TryGetValue(entry.Key, out DrawingColor cachedColor)
				? cachedColor
				: DrawingColor.DarkCyan;
			level.Add(new CatalogNode
			{
				Name = title,
				NodeType = entry.Key,
				NodeColor = nodeColor
			});
		}

		SortCatalog(roots);
		return roots;
	}

	private static void SortCatalog(List<CatalogNode> nodes)
	{
		nodes.Sort((left, right) =>
		{
			if ((left.NodeType == null) != (right.NodeType == null))
			{
				return left.NodeType == null ? -1 : 1;
			}
			return StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name);
		});
		foreach (CatalogNode node in nodes)
		{
			SortCatalog(node.Children);
		}
	}

	private TreeViewItem CreateTreeItem(CatalogNode node)
	{
		var item = new TreeViewItem
		{
			Header = CreateTreeHeader(node),
			Tag = node.NodeType,
			IsExpanded = m_search_text.Length > 0,
			Foreground = ToBrush(m_fore_color)
		};
		foreach (CatalogNode child in node.Children)
		{
			item.Items.Add(CreateTreeItem(child));
		}
		return item;
	}

	private Grid CreateTreeHeader(CatalogNode node)
	{
		var grid = new Grid { MinHeight = 26 };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var icon = new Border
		{
			Width = 12,
			Height = 12,
			Margin = new Thickness(2, 0, 7, 0),
			VerticalAlignment = VerticalAlignment.Center,
			BorderThickness = new Thickness(1),
			BorderBrush = ToBrush(node.NodeType == null
				? DrawingColor.Goldenrod
				: m_auto_color ? node.NodeColor : DrawingColor.DarkCyan),
			Background = node.NodeType == null ? MediaBrushes.Transparent : MediaBrushes.LightGray
		};
		grid.Children.Add(icon);

		string displayName = node.NodeType == null ? Lang.GetOrDefault(node.Name) : node.Name;
		var name = new TextBlock
		{
			Text = displayName,
			VerticalAlignment = VerticalAlignment.Center,
			TextTrimming = TextTrimming.CharacterEllipsis,
			Foreground = ToBrush(m_search_text.Length > 0
				&& displayName.IndexOf(m_search_text, StringComparison.CurrentCultureIgnoreCase) >= 0
					? m_highlight_text_color
					: m_fore_color)
		};
		Grid.SetColumn(name, 1);
		grid.Children.Add(name);

		if (node.NodeType == null && m_show_folder_count)
		{
			var count = new TextBlock
			{
				Text = $"[{node.NodeCount}]",
				VerticalAlignment = VerticalAlignment.Center,
				Foreground = ToBrush(m_folder_count_color),
				Margin = new Thickness(8, 0, 4, 0)
			};
			Grid.SetColumn(count, 2);
			grid.Children.Add(count);
		}
		else if (node.NodeType != null && m_show_info_button)
		{
			var info = new Button
			{
				Content = "ⓘ",
				Foreground = ToBrush(m_auto_color ? node.NodeColor : m_info_button_color),
				Background = MediaBrushes.Transparent,
				BorderThickness = new Thickness(0),
				Padding = new Thickness(5, 0, 5, 0),
				Margin = new Thickness(6, 0, 0, 0),
				ToolTip = "预览节点和属性"
			};
			info.Click += (s, e) =>
			{
				ShowPreview(node.NodeType, info);
				e.Handled = true;
			};
			Grid.SetColumn(info, 2);
			grid.Children.Add(info);
		}
		return grid;
	}

	private bool MatchesSearch(Type type, string path)
	{
		if (string.IsNullOrWhiteSpace(m_search_text))
		{
			return true;
		}
		if (type.Name.IndexOf(m_search_text, StringComparison.CurrentCultureIgnoreCase) >= 0
			|| path.IndexOf(m_search_text, StringComparison.CurrentCultureIgnoreCase) >= 0
			|| path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
				.Any(segment => Lang.GetOrDefault(segment).IndexOf(m_search_text, StringComparison.CurrentCultureIgnoreCase) >= 0))
		{
			return true;
		}
		return m_dic_node_title.TryGetValue(type, out string title)
			&& title.IndexOf(m_search_text, StringComparison.CurrentCultureIgnoreCase) >= 0;
	}

	private void OnTreeMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		m_drag_start = e.GetPosition(m_tree);
		m_drag_type = GetNodeType(e.OriginalSource as DependencyObject);
	}

	private void OnTreeMouseMove(object sender, MouseEventArgs e)
	{
		if (e.LeftButton != MouseButtonState.Pressed || m_drag_type == null)
		{
			return;
		}
		WpfPoint point = e.GetPosition(m_tree);
		if (Math.Abs(point.X - m_drag_start.X) < SystemParameters.MinimumHorizontalDragDistance
			&& Math.Abs(point.Y - m_drag_start.Y) < SystemParameters.MinimumVerticalDragDistance)
		{
			return;
		}

		var data = new System.Windows.DataObject();
		data.SetData("STNodeType", m_drag_type);
		System.Windows.DragDrop.DoDragDrop(m_tree, data, DragDropEffects.Copy);
		m_drag_type = null;
	}

	private Type GetNodeType(DependencyObject source)
	{
		TreeViewItem item = ItemsControl.ContainerFromElement(m_tree, source) as TreeViewItem;
		return item?.Tag as Type;
	}

	private void ShowPreview(Type nodeType, UIElement placementTarget)
	{
		try
		{
			_editor.Nodes.Clear();
			if (Activator.CreateInstance(nodeType) is not STNode node)
			{
				return;
			}
			node.Left = 30;
			node.Top = 30;
			_editor.Nodes.Add(node);
			_editor.SetActiveNode(node);
			_editor.FitCanvasToNodes();
			_property_grid.SetNode(node);
			m_preview_popup.PlacementTarget = placementTarget;
			m_preview_popup.Placement = m_info_panel_is_left_layout
				? PlacementMode.Left
				: PlacementMode.Right;
			m_preview_popup.IsOpen = true;
		}
		catch
		{
			m_preview_popup.IsOpen = false;
		}
	}

	private void ApplyColors()
	{
		Background = ToBrush(m_back_color);
		Foreground = ToBrush(m_fore_color);
		m_search_box.Background = ToBrush(m_text_box_color);
		m_search_box.Foreground = ToBrush(m_fore_color);
		m_clear_button.Foreground = ToBrush(m_fore_color);
		m_tree.Background = ToBrush(m_item_back_color);
		m_tree.Foreground = ToBrush(m_fore_color);
		if (m_preview_popup.Child is Border preview)
		{
			preview.Background = ToBrush(m_back_color);
		}
	}

	private void ApplyFont()
	{
		MediaFontFamily family = new MediaFontFamily(m_font.Name);
		double size = Math.Max(1d, m_font.SizeInPoints * 96d / 72d);
		FontFamily = family;
		FontSize = size;
		m_search_box.FontFamily = family;
		m_search_box.FontSize = size;
		m_tree.FontFamily = family;
		m_tree.FontSize = size;
	}

	private static SolidColorBrush ToBrush(DrawingColor color)
	{
		var brush = new SolidColorBrush(MediaColor.FromArgb(color.A, color.R, color.G, color.B));
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
		m_preview_popup.IsOpen = false;
		m_search_box.TextChanged -= OnSearchTextChanged;
		m_clear_button.Click -= OnClearSearchClick;
		m_tree.PreviewMouseLeftButtonDown -= OnTreeMouseLeftButtonDown;
		m_tree.PreviewMouseMove -= OnTreeMouseMove;
		_editor.Dispose();
		_property_grid.Dispose();
		m_font?.Dispose();
		m_font = null;
		GC.SuppressFinalize(this);
	}
}
