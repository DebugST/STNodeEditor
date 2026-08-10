using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DrawingPoint = System.Drawing.Point;
using WpfPoint = System.Windows.Point;

namespace WinNodeEditorDemo.Blender
{
    /// <summary>
    /// Native WPF popup used by the MixRGB node's enum control.
    /// </summary>
    public class FrmEnumSelect : Window
    {
        private readonly DrawingPoint _screenPoint;
        private readonly ListBox _listBox;
        private bool _isClosing;

        public Enum Enum { get; private set; }

        public FrmEnumSelect(Enum value, DrawingPoint screenPoint, int width, float scale)
        {
            Enum = value ?? throw new ArgumentNullException(nameof(value));
            _screenPoint = screenPoint;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Background = new SolidColorBrush(Color.FromRgb(34, 34, 34));
            Width = Math.Max(80, width * scale);

            var values = new List<object>();
            foreach (object item in System.Enum.GetValues(value.GetType()))
            {
                values.Add(item);
            }
            Height = Math.Max(24, values.Count * 24 * scale);

            Window? owner = Application.Current?.MainWindow;
            if (owner != null && !ReferenceEquals(owner, this))
            {
                Owner = owner;
            }

            _listBox = new ListBox
            {
                Background = Background,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                FontSize = Math.Max(11, 12 * scale)
            };
            foreach (object item in values)
            {
                _listBox.Items.Add(item);
            }
            _listBox.SelectedItem = value;
            _listBox.SelectionChanged += OnSelectionChanged;
            Content = _listBox;

            Loaded += OnLoaded;
            Deactivated += OnDeactivated;
            Closing += (_, _) =>
            {
                _isClosing = true;
                Deactivated -= OnDeactivated;
            };
            PreviewKeyDown += OnPreviewKeyDown;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Window? owner = Owner ?? Application.Current?.MainWindow;
            if (owner != null && !ReferenceEquals(owner, this))
            {
                PresentationSource? source = PresentationSource.FromVisual(owner);
                WpfPoint screenPoint = new WpfPoint(_screenPoint.X, _screenPoint.Y);
                WpfPoint screenDip = source?.CompositionTarget != null
                    ? source.CompositionTarget.TransformFromDevice.Transform(screenPoint)
                    : new WpfPoint(
                        screenPoint.X / VisualTreeHelper.GetDpi(owner).DpiScaleX,
                        screenPoint.Y / VisualTreeHelper.GetDpi(owner).DpiScaleY);
                Left = screenDip.X;
                Top = screenDip.Y;
            }
            else
            {
                Left = _screenPoint.X;
                Top = _screenPoint.Y;
            }

            _listBox.Focus();
            _listBox.ScrollIntoView(_listBox.SelectedItem);
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _listBox.SelectedItem is not Enum selected)
            {
                return;
            }

            Enum = selected;
            DialogResult = true;
        }

        private void OnDeactivated(object? sender, EventArgs e)
        {
            if (IsVisible && DialogResult != true)
            {
                CloseOnce();
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
            {
                return;
            }

            e.Handled = true;
            CloseOnce();
        }

        private void CloseOnce()
        {
            if (_isClosing)
            {
                return;
            }

            _isClosing = true;
            Deactivated -= OnDeactivated;
            Close();
        }
    }
}
