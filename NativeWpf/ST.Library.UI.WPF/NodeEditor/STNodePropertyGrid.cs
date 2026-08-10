using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DrawingColor = System.Drawing.Color;
using DrawingFont = System.Drawing.Font;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using MediaFontFamily = System.Windows.Media.FontFamily;

namespace ST.Library.UI.NodeEditor;

/// <summary>
/// WPF property editor for <see cref="STNodePropertyAttribute"/> values.
/// </summary>
public class STNodePropertyGrid : UserControl, IDisposable
{
	private readonly Border m_title_border;
	private readonly TextBlock m_title_text;
	private readonly Button m_switch_button;
	private readonly Border m_error_border;
	private readonly TextBlock m_error_text;
	private readonly ScrollViewer m_scroll_viewer;
	private readonly StackPanel m_content;
	private readonly Border m_description_border;
	private readonly TextBlock m_description_text;
	private readonly List<STNodePropertyDescriptor> m_descriptors = new List<STNodePropertyDescriptor>();
	private readonly Dictionary<STNodePropertyDescriptor, STNodePropertyValueHost> m_value_hosts = new Dictionary<STNodePropertyDescriptor, STNodePropertyValueHost>();
	private readonly string[] m_info_keys = new string[4] { "作者", "邮箱", "链接", "查看帮助" };

	private STNode _node;
	private STNodeAttribute m_node_attribute;
	private bool m_show_info;
	private bool m_show_title = true;
	private bool m_auto_color = true;
	private bool m_info_first_on_draw = true;
	private bool m_read_only_model;
	private bool m_is_edit_enable = true;
	private bool m_disposed;
	private string m_error_message;
	private DrawingFont m_font = new DrawingFont("Segoe UI", 9f);

	private DrawingColor m_item_hover_color = DrawingColor.FromArgb(50, 125, 125, 125);
	private DrawingColor m_item_selected_color = DrawingColor.DodgerBlue;
	private DrawingColor m_item_value_back_color = DrawingColor.FromArgb(255, 50, 50, 50);
	private DrawingColor m_title_color = DrawingColor.FromArgb(255, 60, 60, 60);
	private DrawingColor m_error_color = DrawingColor.IndianRed;
	private DrawingColor m_description_color = DrawingColor.Gray;
	private DrawingColor m_back_color = DrawingColor.FromArgb(255, 35, 35, 35);
	private DrawingColor m_fore_color = DrawingColor.FromArgb(255, 220, 220, 220);

	[Browsable(false)]
	public STNode STNode => _node;

	public DrawingColor ItemHoverColor
	{
		get => m_item_hover_color;
		set => m_item_hover_color = value;
	}

	public DrawingColor ItemSelectedColor
	{
		get => m_item_selected_color;
		set => m_item_selected_color = value;
	}

	public DrawingColor ItemValueBackColor
	{
		get => m_item_value_back_color;
		set
		{
			m_item_value_back_color = value;
			RebuildContent();
		}
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

	public DrawingColor ErrorColor
	{
		get => m_error_color;
		set
		{
			m_error_color = value;
			ApplyColors();
		}
	}

	public DrawingColor DescriptionColor
	{
		get => m_description_color;
		set
		{
			m_description_color = value;
			ApplyColors();
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
			if (value == null || ReferenceEquals(m_font, value))
			{
				return;
			}
			m_font.Dispose();
			m_font = value;
			ApplyFont();
		}
	}

	public string Text { get; set; } = "NodeProperty";

	[DefaultValue(true)]
	public bool ShowTitle
	{
		get => m_show_title;
		set
		{
			m_show_title = value;
			m_title_border.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
		}
	}

	[DefaultValue(true)]
	public bool AutoColor
	{
		get => m_auto_color;
		set
		{
			m_auto_color = value;
			ApplyColors();
		}
	}

	[DefaultValue(true)]
	public bool InfoFirstOnDraw
	{
		get => m_info_first_on_draw;
		set => m_info_first_on_draw = value;
	}

	[DefaultValue(false)]
	public bool ReadOnlyModel
	{
		get => m_read_only_model;
		set
		{
			m_read_only_model = value;
			RebuildContent();
		}
	}

	[Browsable(false)]
	public int ScrollOffset => -(int)Math.Round(m_scroll_viewer.VerticalOffset);

	[DefaultValue(true)]
	public bool IsEditEnable
	{
		get => m_is_edit_enable;
		set
		{
			m_is_edit_enable = value;
			if (_node != null)
			{
				BuildDescriptors();
				RebuildContent();
			}
		}
	}

	public STNodePropertyGrid()
	{
		Focusable = false;
		MinWidth = 120;
		MinHeight = 50;

		var root = new DockPanel();
		Content = root;

		var title_grid = new Grid();
		title_grid.ColumnDefinitions.Add(new ColumnDefinition());
		title_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		m_title_text = new TextBlock
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			TextTrimming = TextTrimming.CharacterEllipsis,
			Margin = new Thickness(8, 2, 4, 2)
		};
		title_grid.Children.Add(m_title_text);
		m_switch_button = new Button
		{
			Content = "↔",
			MinWidth = 24,
			Padding = new Thickness(4, 0, 4, 0),
			Margin = new Thickness(2),
			Visibility = Visibility.Collapsed,
			ToolTip = "切换节点信息与属性"
		};
		m_switch_button.Click += OnSwitchClick;
		Grid.SetColumn(m_switch_button, 1);
		title_grid.Children.Add(m_switch_button);
		m_title_border = new Border
		{
			MinHeight = 24,
			Child = title_grid
		};
		DockPanel.SetDock(m_title_border, Dock.Top);
		root.Children.Add(m_title_border);

		m_error_text = new TextBlock
		{
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(6, 4, 6, 4)
		};
		m_error_border = new Border
		{
			Child = m_error_text,
			Visibility = Visibility.Collapsed
		};
		DockPanel.SetDock(m_error_border, Dock.Top);
		root.Children.Add(m_error_border);

		m_description_text = new TextBlock
		{
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(6, 4, 6, 4)
		};
		m_description_border = new Border
		{
			Child = m_description_text,
			Visibility = Visibility.Collapsed
		};
		DockPanel.SetDock(m_description_border, Dock.Bottom);
		root.Children.Add(m_description_border);

		m_content = new StackPanel();
		m_scroll_viewer = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Content = m_content
		};
		root.Children.Add(m_scroll_viewer);

		ApplyColors();
		ApplyFont();
		UpdateTitle();
	}

	public void SetNode(STNode node)
	{
		_node = node;
		m_node_attribute = node?.GetType()
			.GetCustomAttributes(typeof(STNodeAttribute), inherit: true)
			.OfType<STNodeAttribute>()
			.FirstOrDefault();
		m_error_message = null;
		m_show_info = node != null && (m_info_first_on_draw || !HasVisibleProperties(node));
		m_scroll_viewer.ScrollToTop();
		BuildDescriptors();
		UpdateTitle();
		UpdateError();
		RebuildContent();
		ApplyColors();
	}

	public void SetInfoKey(string author, string mail, string link, string help)
	{
		m_info_keys[0] = author;
		m_info_keys[1] = mail;
		m_info_keys[2] = link;
		m_info_keys[3] = help;
		if (m_show_info)
		{
			RebuildContent();
		}
	}

	public void SetErrorMessage(string text)
	{
		m_error_message = text;
		UpdateError();
	}

	public void Invalidate(Rectangle rectangle)
	{
		InvalidateVisual();
		foreach (STNodePropertyValueHost host in m_value_hosts.Values)
		{
			host.InvalidatePresenter();
		}
	}

	private void OnSwitchClick(object sender, RoutedEventArgs e)
	{
		m_show_info = !m_show_info;
		m_scroll_viewer.ScrollToTop();
		RebuildContent();
	}

	private bool HasVisibleProperties(STNode node)
	{
		return node.GetType().GetProperties().Any(property =>
		{
			var attribute = property.GetCustomAttributes(typeof(STNodePropertyAttribute), inherit: true)
				.OfType<STNodePropertyAttribute>()
				.FirstOrDefault();
			return attribute != null && (m_is_edit_enable || !attribute.IsHide);
		});
	}

	private void BuildDescriptors()
	{
		m_descriptors.Clear();
		if (_node == null)
		{
			return;
		}

		foreach (PropertyInfo property in _node.GetType().GetProperties())
		{
			var attribute = property.GetCustomAttributes(typeof(STNodePropertyAttribute), inherit: true)
				.OfType<STNodePropertyAttribute>()
				.FirstOrDefault();
			if (attribute == null || (!m_is_edit_enable && attribute.IsHide))
			{
				continue;
			}

			if (Activator.CreateInstance(attribute.DescriptorType) is not STNodePropertyDescriptor descriptor)
			{
				throw new ArgumentException("[STNodePropertyAttribute.DescriptorType]参数值必须为[STNodePropertyDescriptor]或者其子类的类型");
			}

			descriptor.Node = _node;
			descriptor.Name = Lang.Get(attribute.Name);
			descriptor.Description = Lang.GetOrDefault(attribute.Description);
			descriptor.PropertyInfo = property;
			descriptor.IsEditEnable = m_is_edit_enable || attribute.IsEditEnable;
			descriptor.IsReadOnly = attribute.IsReadOnly;
			descriptor.Control = this;
			m_descriptors.Add(descriptor);
		}
	}

	private void RebuildContent()
	{
		if (m_content == null)
		{
			return;
		}

		m_value_hosts.Clear();
		m_content.Children.Clear();
		if (_node == null)
		{
			m_switch_button.Visibility = Visibility.Collapsed;
			return;
		}

		m_switch_button.Visibility = m_node_attribute != null && m_descriptors.Count > 0
			? Visibility.Visible
			: Visibility.Collapsed;
		if (m_show_info)
		{
			BuildInfoPanel();
			return;
		}

		for (int index = 0; index < m_descriptors.Count; index++)
		{
			m_content.Children.Add(CreatePropertyRow(m_descriptors[index], index));
		}
	}

	private Grid CreatePropertyRow(STNodePropertyDescriptor descriptor, int index)
	{
		var row = new Grid
		{
			MinHeight = 32,
			Background = index % 2 == 0
				? ToBrush(DrawingColor.FromArgb(20, 0, 0, 0))
				: ToBrush(DrawingColor.FromArgb(20, 255, 255, 255))
		};
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });

		var name = new TextBlock
		{
			Text = descriptor.Name,
			VerticalAlignment = VerticalAlignment.Center,
			TextAlignment = TextAlignment.Right,
			TextTrimming = TextTrimming.CharacterEllipsis,
			Margin = new Thickness(4)
		};
		name.MouseEnter += (s, e) => ShowDescription(descriptor.Description);
		name.MouseLeave += (s, e) => HideDescription();
		row.Children.Add(name);

		FrameworkElement editor = CreateValueEditor(descriptor, row);
		Grid.SetColumn(editor, 1);
		row.Children.Add(editor);
		ApplyFont(row);
		return row;
	}

	private FrameworkElement CreateValueEditor(STNodePropertyDescriptor descriptor, Grid row)
	{
		bool readOnly = m_read_only_model || descriptor.IsReadOnly || !descriptor.IsEditEnable;
		if (descriptor.GetType() != typeof(STNodePropertyDescriptor))
		{
			var host = new STNodePropertyValueHost(this, descriptor, row, readOnly);
			m_value_hosts[descriptor] = host;
			return host;
		}

		Type propertyType = descriptor.PropertyInfo.PropertyType;
		if (propertyType == typeof(bool))
		{
			var checkBox = new CheckBox
			{
				IsChecked = descriptor.GetValue(null) is bool value && value,
				IsEnabled = !readOnly,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(8, 2, 4, 2)
			};
			checkBox.Click += (s, e) => CommitValue(descriptor, checkBox.IsChecked == true);
			return checkBox;
		}

		if (propertyType.IsEnum)
		{
			var comboBox = new ComboBox
			{
				ItemsSource = Enum.GetNames(propertyType),
				SelectedItem = descriptor.GetValue(null)?.ToString(),
				IsEnabled = !readOnly,
				Margin = new Thickness(4, 3, 4, 3)
			};
			comboBox.SelectionChanged += (s, e) =>
			{
				if (comboBox.SelectedItem is string text)
				{
					CommitText(descriptor, text);
				}
			};
			return comboBox;
		}

		var textBox = new TextBox
		{
			Text = descriptor.GetStringFromValue() ?? string.Empty,
			IsReadOnly = readOnly,
			VerticalContentAlignment = VerticalAlignment.Center,
			Background = ToBrush(m_item_value_back_color),
			Foreground = ToBrush(m_fore_color),
			BorderThickness = new Thickness(0),
			Margin = new Thickness(4, 3, 4, 3),
			Padding = new Thickness(4, 1, 4, 1)
		};
		if (!readOnly)
		{
			textBox.KeyDown += (s, e) =>
			{
				if (e.Key == Key.Enter)
				{
					CommitText(descriptor, textBox.Text);
					e.Handled = true;
				}
			};
			textBox.LostKeyboardFocus += (s, e) => CommitText(descriptor, textBox.Text);
		}

		if (descriptor.GetType() == typeof(STNodePropertyDescriptor) || readOnly)
		{
			return textBox;
		}

		var panel = new Grid();
		panel.ColumnDefinitions.Add(new ColumnDefinition());
		panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		panel.Children.Add(textBox);
		var customButton = new Button
		{
			Content = "…",
			MinWidth = 24,
			Margin = new Thickness(0, 3, 4, 3),
			Padding = new Thickness(3, 0, 3, 0)
		};
		customButton.Click += (s, e) =>
		{
			try
			{
				var location = new System.Drawing.Point(descriptor.RectangleR.Right - 12, descriptor.RectangleR.Top + descriptor.RectangleR.Height / 2);
				descriptor.OnMouseClick(new STNodeMouseEventArgs(STMouseButtons.Left, 1, location.X, location.Y, 0));
				textBox.Text = descriptor.GetStringFromValue() ?? string.Empty;
				_node?.Owner?.Invalidate();
				SetErrorMessage(null);
			}
			catch (Exception ex)
			{
				descriptor.OnSetValueError(ex);
			}
		};
		Grid.SetColumn(customButton, 1);
		panel.Children.Add(customButton);
		return panel;
	}

	internal void CommitText(STNodePropertyDescriptor descriptor, string text)
	{
		try
		{
			descriptor.SetValue(text);
			_node?.Owner?.Invalidate();
			SetErrorMessage(null);
			RefreshDescriptor(descriptor);
		}
		catch (Exception ex)
		{
			descriptor.OnSetValueError(ex);
		}
	}

	internal void BeginEdit(STNodePropertyDescriptor descriptor)
	{
		if (m_read_only_model || descriptor == null)
		{
			return;
		}
		if (m_value_hosts.TryGetValue(descriptor, out STNodePropertyValueHost host))
		{
			host.BeginEdit();
		}
	}

	internal void RefreshDescriptor(STNodePropertyDescriptor descriptor)
	{
		if (descriptor != null && m_value_hosts.TryGetValue(descriptor, out STNodePropertyValueHost host))
		{
			host.InvalidatePresenter();
		}
		_node?.Owner?.Invalidate();
	}

	internal void UpdateDescriptorLayout(
		STNodePropertyDescriptor descriptor,
		Grid row,
		FrameworkElement valueElement)
	{
		if (descriptor == null
			|| row == null
			|| valueElement == null
			|| !m_content.IsAncestorOf(row)
			|| row.ActualWidth <= 0d
			|| row.ActualHeight <= 0d
			|| valueElement.ActualWidth <= 0d
			|| valueElement.ActualHeight <= 0d)
		{
			return;
		}

		System.Windows.Point rowOrigin = row.TranslatePoint(new System.Windows.Point(0d, 0d), m_content);
		System.Windows.Point valueOrigin = valueElement.TranslatePoint(new System.Windows.Point(0d, 0d), row);
		int titleHeight = m_show_title ? (int)Math.Round(m_title_border.ActualHeight) : 0;
		int rowTop = titleHeight + (int)Math.Round(rowOrigin.Y);
		int rowHeight = Math.Max(1, (int)Math.Round(row.ActualHeight));
		int valueLeft = (int)Math.Round(valueOrigin.X);
		int valueTop = rowTop + (int)Math.Round(valueOrigin.Y);
		var rectangle = new Rectangle(0, rowTop, Math.Max(1, (int)Math.Round(row.ActualWidth)), rowHeight);
		var rectangleLeft = new Rectangle(0, rowTop, Math.Max(0, valueLeft), rowHeight);
		var rectangleRight = new Rectangle(
			valueLeft,
			valueTop,
			Math.Max(1, (int)Math.Round(valueElement.ActualWidth)),
			Math.Max(1, (int)Math.Round(valueElement.ActualHeight)));
		if (descriptor.Rectangle == rectangle
			&& descriptor.RectangleL == rectangleLeft
			&& descriptor.RectangleR == rectangleRight)
		{
			return;
		}

		descriptor.Rectangle = rectangle;
		descriptor.RectangleL = rectangleLeft;
		descriptor.RectangleR = rectangleRight;
		try
		{
			descriptor.OnSetItemLocation();
		}
		catch (Exception ex)
		{
			descriptor.OnSetValueError(ex);
		}
	}

	private void CommitValue(STNodePropertyDescriptor descriptor, object value)
	{
		try
		{
			descriptor.SetValue(value);
			_node?.Owner?.Invalidate();
			SetErrorMessage(null);
		}
		catch (Exception ex)
		{
			descriptor.OnSetValueError(ex);
		}
	}

	private void BuildInfoPanel()
	{
		if (m_node_attribute == null)
		{
			return;
		}

		AddInfoRow(m_info_keys[0], m_node_attribute.Author);
		AddInfoRow(m_info_keys[1], m_node_attribute.Mail);
		AddInfoRow(m_info_keys[2], m_node_attribute.Link, isLink: true);

		if (!string.IsNullOrWhiteSpace(m_node_attribute.DisplayDescription))
		{
			m_content.Children.Add(new TextBlock
			{
				Text = m_node_attribute.DisplayDescription,
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(8)
			});
		}

		var helpButton = new Button
		{
			Content = m_info_keys[3],
			Margin = new Thickness(8),
			IsEnabled = STNodeAttribute.GetHelpMethod(_node.GetType()) != null
		};
		helpButton.Click += (s, e) =>
		{
			try
			{
				STNodeAttribute.ShowHelp(_node.GetType());
			}
			catch (Exception ex)
			{
				SetErrorMessage(ex.Message);
			}
		};
		m_content.Children.Add(helpButton);
		ApplyFont(m_content);
	}

	private void AddInfoRow(string key, string value, bool isLink = false)
	{
		var row = new Grid { MinHeight = 30 };
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
		row.Children.Add(new TextBlock
		{
			Text = key,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(6)
		});
		FrameworkElement valueElement;
		if (isLink && !string.IsNullOrWhiteSpace(value))
		{
			var button = new Button
			{
				Content = value,
				HorizontalContentAlignment = HorizontalAlignment.Left,
				BorderThickness = new Thickness(0),
				Background = MediaBrushes.Transparent,
				Foreground = MediaBrushes.CornflowerBlue,
				Padding = new Thickness(4),
				Cursor = Cursors.Hand
			};
			button.Click += (s, e) =>
			{
				try
				{
					Process.Start(new ProcessStartInfo(value) { UseShellExecute = true });
				}
				catch (Exception ex)
				{
					SetErrorMessage(ex.Message);
				}
			};
			valueElement = button;
		}
		else
		{
			valueElement = new TextBlock
			{
				Text = value ?? string.Empty,
				VerticalAlignment = VerticalAlignment.Center,
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(6),
				Opacity = 0.75
			};
		}
		Grid.SetColumn(valueElement, 1);
		row.Children.Add(valueElement);
		m_content.Children.Add(row);
	}

	private void UpdateTitle()
	{
		m_title_text.Text = _node?.Title ?? Text;
	}

	private void UpdateError()
	{
		m_error_text.Text = m_error_message ?? string.Empty;
		m_error_border.Visibility = string.IsNullOrWhiteSpace(m_error_message)
			? Visibility.Collapsed
			: Visibility.Visible;
	}

	private void ShowDescription(string description)
	{
		if (string.IsNullOrWhiteSpace(description))
		{
			return;
		}
		m_description_text.Text = description;
		m_description_border.Visibility = Visibility.Visible;
	}

	private void HideDescription()
	{
		m_description_border.Visibility = Visibility.Collapsed;
	}

	private void ApplyColors()
	{
		Background = ToBrush(m_back_color);
		Foreground = ToBrush(m_fore_color);
		DrawingColor title = m_auto_color && _node != null ? _node.TitleColor : m_title_color;
		m_title_border.Background = ToBrush(title);
		m_error_border.Background = ToBrush(DrawingColor.FromArgb(210, m_error_color));
		m_description_border.Background = ToBrush(DrawingColor.FromArgb(210, m_description_color));
		foreach (STNodePropertyValueHost host in m_value_hosts.Values)
		{
			host.InvalidatePresenter();
		}
	}

	private void ApplyFont()
	{
		ApplyFont(this);
		foreach (STNodePropertyValueHost host in m_value_hosts.Values)
		{
			host.InvalidatePresenter();
		}
	}

	private void ApplyFont(DependencyObject root)
	{
		if (root is Control control)
		{
			control.FontFamily = new MediaFontFamily(m_font.Name);
			control.FontSize = Math.Max(1d, m_font.SizeInPoints * 96d / 72d);
		}
		else if (root is TextBlock text)
		{
			text.FontFamily = new MediaFontFamily(m_font.Name);
			text.FontSize = Math.Max(1d, m_font.SizeInPoints * 96d / 72d);
		}

		int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			ApplyFont(System.Windows.Media.VisualTreeHelper.GetChild(root, i));
		}
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
		m_switch_button.Click -= OnSwitchClick;
		m_content.Children.Clear();
		m_descriptors.Clear();
		m_value_hosts.Clear();
		m_font?.Dispose();
		m_font = null;
		GC.SuppressFinalize(this);
	}
}
