using Microsoft.Win32;
using ColorVision.UI;
using ST.Library.UI.NodeEditor;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WinNodeEditorDemo.NumberNode;
using DrawingPoint = System.Drawing.Point;

namespace WpfNodeEditorDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// </summary>
    public partial class MainWindow : Window
    {
        private STNodeEditorHelper? _editorHelper;
        private readonly STNodePropertyMetadataProvider _propertyMetadataProvider = new STNodePropertyMetadataProvider();
        private STNode? _propertyNode;
        private string? _flowFile;
        private bool _initialized;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            Assembly demoAssembly = Assembly.GetExecutingAssembly();
            STNodeEditorMain.LoadAssembly(demoAssembly);

            _editorHelper = new STNodeEditorHelper(STNodeEditorMain, demoAssembly);
            _editorHelper.PropertyEditorRequested += EditorHelper_PropertyEditorRequested;
            STNodeEditorMain.ActiveChanged += STNodeEditorMain_ActiveChanged;
            STNodeEditorMain.SelectedChanged += STNodeEditorMain_SelectedChanged;
            STNodeEditorMain.CanvasMoved += EditorViewportChanged;
            STNodeEditorMain.CanvasScaled += EditorViewportChanged;
            STNodeEditorMain.NodeLocationChanged += EditorViewportChanged;
            STNodeEditorMain.NodeRemoved += STNodeEditorMain_NodeRemoved;
            OverlayCanvas.SizeChanged += OverlayCanvas_SizeChanged;
            PropertyFlyout.SizeChanged += PropertyFlyout_SizeChanged;
            DataContext = _editorHelper;

            if (STNodeEditorMain.Nodes.Count == 0)
            {
                CreateSampleCanvas();
            }
        }

        private void CreateSampleCanvas()
        {
            var first = new NumberInputNode { Number = 8 };
            var second = new NumberInputNode { Number = 12 };
            var third = new NumberInputNode { Number = 20 };
            var firstSum = new NumberAddNode();
            var total = new NumberAddNode();
            var attributes = new WinNodeEditorDemo.AttrTestNode
            {
                Color = System.Drawing.Color.CornflowerBlue,
                Bool = true,
                Int = 42,
                Float = 3.14f
            };

            STNodeEditorMain.Nodes.Add(first);
            STNodeEditorMain.Nodes.Add(second);
            STNodeEditorMain.Nodes.Add(third);
            STNodeEditorMain.Nodes.Add(firstSum);
            STNodeEditorMain.Nodes.Add(total);
            STNodeEditorMain.Nodes.Add(attributes);

            first.GetAllOutputOptions()[0].ConnectOption(firstSum.GetAllInputOptions()[0]);
            second.GetAllOutputOptions()[0].ConnectOption(firstSum.GetAllInputOptions()[1]);
            firstSum.GetAllOutputOptions()[0].ConnectOption(total.GetAllInputOptions()[0]);
            third.GetAllOutputOptions()[0].ConnectOption(total.GetAllInputOptions()[1]);

            // Re-publish source values after the graph is connected.
            first.Number = first.Number;
            second.Number = second.Number;
            third.Number = third.Number;

            STNodeEditorMain.AddSelectedNode(attributes);
            STNodeEditorMain.SetActiveNode(attributes);
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                _editorHelper?.AutoArrange();
                STNodeEditorMain.ClearHistory();
            }));
        }

        private void STNodeEditorMain_ActiveChanged(object? sender, EventArgs e)
        {
            ShowPropertyEditor(GetSingleSelectedNode());
        }

        private void STNodeEditorMain_SelectedChanged(object? sender, EventArgs e)
        {
            ShowPropertyEditor(GetSingleSelectedNode());
        }

        private void EditorHelper_PropertyEditorRequested(object? sender, EventArgs e)
        {
            ShowPropertyEditor(GetSingleSelectedNode());
        }

        private STNode? GetSingleSelectedNode()
        {
            STNode? selectedNode = null;
            foreach (STNode node in STNodeEditorMain.Nodes)
            {
                if (!node.IsSelected)
                {
                    continue;
                }

                if (selectedNode != null)
                {
                    return null;
                }

                selectedNode = node;
            }

            return ReferenceEquals(selectedNode, STNodeEditorMain.ActiveNode) ? selectedNode : null;
        }

        private void ShowPropertyEditor(STNode? node)
        {
            if (!ReferenceEquals(_propertyNode, node))
            {
                if (_propertyNode != null)
                {
                    _propertyNode.PropertyChanged -= PropertyNode_PropertyChanged;
                }

                _propertyNode = node;
                if (_propertyNode != null)
                {
                    _propertyNode.PropertyChanged += PropertyNode_PropertyChanged;
                }
            }

            if (node == null)
            {
                PropertyFlyout.Visibility = Visibility.Collapsed;
                PropertyEditorHost.Content = null;
                return;
            }

            PropertyTitle.Text = string.IsNullOrWhiteSpace(node.Title) ? node.GetType().Name : node.Title;
            PropertyEditorHost.Content = PropertyEditorHelper.GenPropertyEditorControl(
                node,
                showCategoryHeader: false,
                metadataProvider: _propertyMetadataProvider);
            PropertyFlyout.Visibility = Visibility.Visible;
            UpdatePropertyEditorPosition();
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(UpdatePropertyEditorPosition));
        }

        private void PropertyNode_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_propertyNode == null || e.PropertyName != nameof(STNode.Title))
            {
                return;
            }

            PropertyTitle.Text = string.IsNullOrWhiteSpace(_propertyNode.Title)
                ? _propertyNode.GetType().Name
                : _propertyNode.Title;
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(UpdatePropertyEditorPosition));
        }

        private void CloseProperties_Click(object sender, RoutedEventArgs e)
        {
            PropertyFlyout.Visibility = Visibility.Collapsed;
        }

        private void EditorViewportChanged(object? sender, EventArgs e)
        {
            UpdatePropertyEditorPosition();
        }

        private void STNodeEditorMain_NodeRemoved(object sender, STNodeEditorEventArgs e)
        {
            if (ReferenceEquals(e.Node, _propertyNode))
            {
                ShowPropertyEditor(null);
            }
        }

        private void OverlayCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePropertyEditorPosition();
        }

        private void PropertyFlyout_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePropertyEditorPosition();
        }

        private void UpdatePropertyEditorPosition()
        {
            if (_propertyNode == null || PropertyFlyout.Visibility != Visibility.Visible)
            {
                return;
            }

            DrawingPoint nodeRight = STNodeEditorMain.CanvasToControl(
                new DrawingPoint(_propertyNode.Left + _propertyNode.Width, _propertyNode.Top));
            DrawingPoint nodeLeft = STNodeEditorMain.CanvasToControl(
                new DrawingPoint(_propertyNode.Left, _propertyNode.Top));
            double panelWidth = PropertyFlyout.ActualWidth > 0 ? PropertyFlyout.ActualWidth : PropertyFlyout.Width;
            double panelHeight = PropertyFlyout.ActualHeight > 0 ? PropertyFlyout.ActualHeight : 320;
            double availableWidth = OverlayCanvas.ActualWidth;
            double availableHeight = OverlayCanvas.ActualHeight;

            double left = nodeRight.X + 14;
            if (left + panelWidth > availableWidth - 12)
            {
                left = nodeLeft.X - panelWidth - 14;
            }
            left = Math.Clamp(left, 12, Math.Max(12, availableWidth - panelWidth - 12));

            double top = Math.Clamp(nodeRight.Y, 72, Math.Max(72, availableHeight - panelHeight - 12));
            Canvas.SetLeft(PropertyFlyout, left);
            Canvas.SetTop(PropertyFlyout, top);
        }

        private void AddNode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement placementTarget)
            {
                _editorHelper?.ShowAddNodeMenu(placementTarget);
            }
        }

        private void Execute_Click(object sender, RoutedEventArgs e)
        {
            NumberInputNode[] inputs = STNodeEditorMain.Nodes.Cast<STNode>()
                .OfType<NumberInputNode>()
                .ToArray();
            foreach (NumberInputNode input in inputs)
            {
                input.Number = input.Number;
            }

            string message = inputs.Length > 0
                ? $"示例已执行：刷新 {inputs.Length} 个输入节点"
                : "当前画布没有 NumberInput 示例节点";
            STNodeEditorMain.ShowAlert(
                message,
                System.Drawing.Color.White,
                System.Drawing.Color.FromArgb(220, 36, 143, 81),
                1600,
                AlertLocation.RightBottom,
                true);
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            STNodeEditorMain.Undo();
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            STNodeEditorMain.Redo();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            STNodeEditorMain.DeleteSelectedNodes();
        }

        private void AutoLayout_Click(object sender, RoutedEventArgs e)
        {
            _editorHelper?.AutoArrange();
        }

        private void Properties_Click(object sender, RoutedEventArgs e)
        {
            ShowPropertyEditor(GetSingleSelectedNode());
        }

        private void Button_Click_Open(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                DefaultExt = ".stn",
                Filter = "STNodeEditor canvas (*.stn)|*.stn|All files (*.*)|*.*",
                CheckFileExists = true
            };
            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            OpenFlow(dialog.FileName);
        }

        private void OpenFlow(string flowName)
        {
            try
            {
                STNodeEditorMain.LoadCanvas(flowName);
                _flowFile = flowName;
                STNodeEditorMain.ClearHistory();
                Title = $"STNodeEditor WPF Demo - {Path.GetFileName(flowName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Unable to open canvas", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_Click_New(object sender, RoutedEventArgs e)
        {
            ResetCanvas();
        }

        private void Button_Click_Clear(object sender, RoutedEventArgs e)
        {
            ResetCanvas();
        }

        private void ResetCanvas()
        {
            STNodeEditorMain.Nodes.Clear();
            STNodeEditorMain.ClearHistory();
            _flowFile = null;
            Title = "STNodeEditor WPF Demo";
        }

        private void Button_Click_Save(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(_flowFile))
            {
                var dialog = new SaveFileDialog
                {
                    DefaultExt = ".stn",
                    Filter = "STNodeEditor canvas (*.stn)|*.stn|All files (*.*)|*.*",
                    AddExtension = true,
                    Title = "Save canvas"
                };
                if (dialog.ShowDialog(this) != true)
                {
                    return;
                }
                _flowFile = dialog.FileName;
            }

            try
            {
                string fullPath = Path.GetFullPath(_flowFile);
                string? directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                STNodeEditorMain.SaveCanvas(fullPath);
                STNodeEditorMain.MarkSaved();
                Title = $"STNodeEditor WPF Demo - {Path.GetFileName(fullPath)}";
                MessageBox.Show(this, "Canvas saved.", "STNodeEditor", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Unable to save canvas", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AutoAlignment_Click(object sender, RoutedEventArgs e)
        {
            STNodeEditorMain.FitCanvasToNodes();
        }

        protected override void OnClosed(EventArgs e)
        {
            STNodeEditorMain.ActiveChanged -= STNodeEditorMain_ActiveChanged;
            STNodeEditorMain.SelectedChanged -= STNodeEditorMain_SelectedChanged;
            STNodeEditorMain.CanvasMoved -= EditorViewportChanged;
            STNodeEditorMain.CanvasScaled -= EditorViewportChanged;
            STNodeEditorMain.NodeLocationChanged -= EditorViewportChanged;
            STNodeEditorMain.NodeRemoved -= STNodeEditorMain_NodeRemoved;
            OverlayCanvas.SizeChanged -= OverlayCanvas_SizeChanged;
            PropertyFlyout.SizeChanged -= PropertyFlyout_SizeChanged;
            if (_propertyNode != null)
            {
                _propertyNode.PropertyChanged -= PropertyNode_PropertyChanged;
                _propertyNode = null;
            }
            if (_editorHelper != null)
            {
                _editorHelper.PropertyEditorRequested -= EditorHelper_PropertyEditorRequested;
            }
            _editorHelper?.Dispose();
            STNodeEditorMain.Dispose();
            base.OnClosed(e);
        }
    }
}
