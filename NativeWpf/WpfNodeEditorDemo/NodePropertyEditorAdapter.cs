using ColorVision.UI;
using ST.Library.UI;
using ST.Library.UI.NodeEditor;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using WinNodeEditorDemo.ImageNode;
using DrawingColor = System.Drawing.Color;

namespace WpfNodeEditorDemo
{
    /// <summary>
    /// Adapts the node editor's metadata to ColorVision.UI without changing either library.
    /// </summary>
    internal sealed class STNodePropertyMetadataProvider : IPropertyEditorMetadataProvider
    {
        public bool IsPropertyManaged(PropertyInfo propertyInfo) => GetAttribute(propertyInfo) != null;

        public bool IsBrowsable(PropertyInfo propertyInfo)
        {
            STNodePropertyAttribute? attribute = GetAttribute(propertyInfo);
            return attribute != null && !attribute.IsHide;
        }

        public Type? GetEditorType(PropertyInfo propertyInfo)
        {
            STNodePropertyAttribute? attribute = GetAttribute(propertyInfo);
            if (attribute == null)
            {
                return null;
            }

            if (attribute.IsReadOnly)
            {
                return typeof(STNodeReadOnlyPropertyEditor);
            }

            if (propertyInfo.PropertyType == typeof(DrawingColor))
            {
                return typeof(STNodeDrawingColorPropertyEditor);
            }

            if (typeof(OpenFileDescriptor).IsAssignableFrom(attribute.DescriptorType))
            {
                return typeof(STNodeOpenFilePropertyEditor);
            }

            return null;
        }

        public string? GetDisplayName(PropertyInfo propertyInfo) =>
            GetAttribute(propertyInfo) is STNodePropertyAttribute attribute
                ? Lang.GetOrDefault(attribute.Name)
                : null;

        public string? GetDescription(PropertyInfo propertyInfo) =>
            GetAttribute(propertyInfo) is STNodePropertyAttribute attribute
                ? Lang.GetOrDefault(attribute.Description)
                : null;

        public string? GetCategory(PropertyInfo propertyInfo) => "节点属性";

        private static STNodePropertyAttribute? GetAttribute(PropertyInfo propertyInfo) =>
            propertyInfo.GetCustomAttribute<STNodePropertyAttribute>(inherit: true);
    }

    public sealed class STNodeReadOnlyPropertyEditor : IPropertyEditor
    {
        public DockPanel GenProperties(PropertyInfo property, object obj)
        {
            var panel = CreatePanel(property, obj);
            var binding = new Binding(property.Name)
            {
                Source = obj,
                Mode = BindingMode.OneWay,
                ConverterCulture = CultureInfo.CurrentCulture
            };
            TextBox textBox = PropertyEditorHelper.CreateSmallTextBox(binding);
            textBox.IsReadOnly = true;
            textBox.IsTabStop = false;
            panel.Children.Add(textBox);
            return panel;
        }

        private static DockPanel CreatePanel(PropertyInfo property, object obj)
        {
            var panel = new DockPanel { LastChildFill = true };
            panel.Children.Add(PropertyEditorHelper.CreateLabel(property, PropertyEditorHelper.GetResourceManager(obj)));
            return panel;
        }
    }

    public sealed class STNodeDrawingColorPropertyEditor : IPropertyEditor
    {
        public DockPanel GenProperties(PropertyInfo property, object obj)
        {
            var panel = new DockPanel { LastChildFill = true };
            panel.Children.Add(PropertyEditorHelper.CreateLabel(property, PropertyEditorHelper.GetResourceManager(obj)));

            var button = new Button
            {
                Width = 82,
                Height = 24,
                Margin = new Thickness(5, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                Content = "选择颜色"
            };
            UpdateSwatch(button, property.GetValue(obj));
            button.Click += (_, _) => ShowColorPicker(button, property, obj);
            panel.Children.Add(button);
            return panel;
        }

        private static void ShowColorPicker(Button button, PropertyInfo property, object source)
        {
            DrawingColor current = property.GetValue(source) is DrawingColor value ? value : DrawingColor.Transparent;
            var picker = new HandyControl.Controls.ColorPicker
            {
                SelectedBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(current.A, current.R, current.G, current.B))
            };
            var window = new Window
            {
                Title = "选择颜色",
                Owner = Window.GetWindow(button),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.WidthAndHeight,
                Content = picker
            };
            picker.Confirmed += (_, _) =>
            {
                System.Windows.Media.Color selected = picker.SelectedBrush.Color;
                property.SetValue(source, DrawingColor.FromArgb(selected.A, selected.R, selected.G, selected.B));
                UpdateSwatch(button, property.GetValue(source));
                if (source is STNode node)
                {
                    node.Invalidate();
                }
                window.Close();
            };
            window.Closed += (_, _) => picker.Dispose();
            window.ShowDialog();
        }

        private static void UpdateSwatch(Button button, object? value)
        {
            DrawingColor color = value is DrawingColor drawingColor ? drawingColor : DrawingColor.Transparent;
            button.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
            button.Foreground = color.GetBrightness() < 0.48f ? Brushes.White : Brushes.Black;
        }
    }

    public sealed class STNodeOpenFilePropertyEditor : IPropertyEditor
    {
        public DockPanel GenProperties(PropertyInfo property, object obj)
        {
            var panel = new DockPanel { LastChildFill = true };
            panel.Children.Add(PropertyEditorHelper.CreateLabel(property, PropertyEditorHelper.GetResourceManager(obj)));

            var selectButton = new Button
            {
                Width = 28,
                Height = 24,
                Margin = new Thickness(5, 0, 0, 0),
                Content = "…",
                ToolTip = "选择文件"
            };
            DockPanel.SetDock(selectButton, Dock.Right);
            panel.Children.Add(selectButton);

            var binding = new Binding(property.Name)
            {
                Source = obj,
                Mode = BindingMode.OneWay
            };
            TextBox textBox = PropertyEditorHelper.CreateSmallTextBox(binding);
            textBox.IsReadOnly = true;
            panel.Children.Add(textBox);

            selectButton.Click += (_, _) =>
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*",
                    CheckFileExists = true,
                    FileName = property.GetValue(obj) as string ?? string.Empty
                };
                if (dialog.ShowDialog(Window.GetWindow(selectButton)) != true)
                {
                    return;
                }

                try
                {
                    property.SetValue(obj, dialog.FileName);
                    textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
                    if (obj is STNode node)
                    {
                        node.Invalidate();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(Window.GetWindow(selectButton), ex.Message, "无法打开文件", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            return panel;
        }
    }
}
