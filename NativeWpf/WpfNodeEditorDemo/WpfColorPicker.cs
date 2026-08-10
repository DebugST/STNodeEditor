using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DrawingColor = System.Drawing.Color;
using MediaColor = System.Windows.Media.Color;

namespace WpfNodeEditorDemo
{
    /// <summary>
    /// Small WPF-only ARGB picker used by the original demo color properties.
    /// </summary>
    internal sealed class WpfColorPicker : Window
    {
        private readonly Slider _alpha;
        private readonly Slider _red;
        private readonly Slider _green;
        private readonly Slider _blue;
        private readonly Border _preview;

        private DrawingColor SelectedColor => DrawingColor.FromArgb(
            (int)Math.Round(_alpha.Value),
            (int)Math.Round(_red.Value),
            (int)Math.Round(_green.Value),
            (int)Math.Round(_blue.Value));

        private WpfColorPicker(DrawingColor initialColor)
        {
            Title = "Select color";
            Width = 420;
            Height = 280;
            MinWidth = 360;
            MinHeight = 260;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var root = new Grid { Margin = new Thickness(16) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _preview = new Border
            {
                Height = 48,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 12)
            };
            root.Children.Add(_preview);

            var channels = new Grid();
            channels.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            channels.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            channels.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            for (int i = 0; i < 4; i++)
            {
                channels.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            _alpha = AddChannel(channels, 0, "A", initialColor.A);
            _red = AddChannel(channels, 1, "R", initialColor.R);
            _green = AddChannel(channels, 2, "G", initialColor.G);
            _blue = AddChannel(channels, 3, "B", initialColor.B);
            Grid.SetRow(channels, 1);
            root.Children.Add(channels);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };
            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Margin = new Thickness(0, 0, 8, 0),
                IsDefault = true
            };
            okButton.Click += (_, _) => DialogResult = true;
            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                IsCancel = true
            };
            buttons.Children.Add(okButton);
            buttons.Children.Add(cancelButton);
            Grid.SetRow(buttons, 2);
            root.Children.Add(buttons);

            Content = root;
            UpdatePreview();
        }

        private Slider AddChannel(Grid grid, int row, string label, byte initialValue)
        {
            var name = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(name, row);
            grid.Children.Add(name);

            var valueText = new TextBlock
            {
                Text = initialValue.ToString(),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(valueText, row);
            Grid.SetColumn(valueText, 2);
            grid.Children.Add(valueText);

            var slider = new Slider
            {
                Minimum = 0,
                Maximum = 255,
                Value = initialValue,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(6, 4, 10, 4)
            };
            slider.ValueChanged += (_, _) =>
            {
                valueText.Text = Math.Round(slider.Value).ToString();
                UpdatePreview();
            };
            Grid.SetRow(slider, row);
            Grid.SetColumn(slider, 1);
            grid.Children.Add(slider);
            return slider;
        }

        private void UpdatePreview()
        {
            if (_preview == null || _alpha == null || _red == null || _green == null || _blue == null)
            {
                return;
            }

            MediaColor color = MediaColor.FromArgb(
                (byte)Math.Round(_alpha.Value),
                (byte)Math.Round(_red.Value),
                (byte)Math.Round(_green.Value),
                (byte)Math.Round(_blue.Value));
            _preview.Background = new SolidColorBrush(color);
        }

        public static bool TryPick(DrawingColor initialColor, out DrawingColor selectedColor)
        {
            var dialog = new WpfColorPicker(initialColor);
            Window? owner = Application.Current?.MainWindow;
            if (owner != null && owner.IsVisible)
            {
                dialog.Owner = owner;
            }

            if (dialog.ShowDialog() == true)
            {
                selectedColor = dialog.SelectedColor;
                return true;
            }

            selectedColor = initialColor;
            return false;
        }
    }
}
