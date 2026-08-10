using ST.Library.UI.NodeEditor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using DrawingPoint = System.Drawing.Point;
using DrawingPointF = System.Drawing.PointF;
using DrawingSize = System.Drawing.Size;

namespace WpfNodeEditorDemo
{
    /// <summary>
    /// Provides editor commands and builds the node catalog directly from the demo assembly.
    /// </summary>
    public sealed class STNodeEditorHelper : INotifyPropertyChanged, IDisposable
    {
        private readonly ContextMenu _contextMenu = new ContextMenu();
        private readonly ContextMenu _addNodeMenu = new ContextMenu();
        private readonly IReadOnlyDictionary<Type, string> _nodeTypes;
        private DrawingPoint _contextCanvasPoint;
        private bool _disposed;

        public STNodeEditor STNodeEditor { get; }

        public float CanvasScale
        {
            get => STNodeEditor.CanvasScale;
            set
            {
                float scale = Math.Clamp(value, 0.2f, 5f);
                if (Math.Abs(STNodeEditor.CanvasScale - scale) < 0.001f)
                {
                    return;
                }

                DrawingSize clientSize = STNodeEditor.ClientSize;
                STNodeEditor.ScaleCanvas(scale, clientSize.Width / 2f, clientSize.Height / 2f);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler? PropertyEditorRequested;

        public STNodeEditorHelper(STNodeEditor nodeEditor, Assembly nodeAssembly)
        {
            STNodeEditor = nodeEditor ?? throw new ArgumentNullException(nameof(nodeEditor));
            ArgumentNullException.ThrowIfNull(nodeAssembly);
            _nodeTypes = nodeAssembly.GetTypes()
                .Where(type => type.IsSubclassOf(typeof(STNode)) && !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null)
                .ToDictionary(
                    type => type,
                    type => type.GetCustomAttribute<STNodeAttribute>(inherit: false)?.Path ?? string.Empty);

            STNodeEditor.CanvasScaled += OnCanvasScaled;
            _contextMenu.Opened += OnContextMenuOpened;
            STNodeEditor.ContextMenu = _contextMenu;
        }

        private void OnCanvasScaled(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(CanvasScale));
        }

        private void OnContextMenuOpened(object sender, RoutedEventArgs e)
        {
            System.Windows.Point position = Mouse.GetPosition(STNodeEditor);
            var controlPoint = new DrawingPoint((int)Math.Round(position.X), (int)Math.Round(position.Y));
            _contextCanvasPoint = STNodeEditor.ControlToCanvas(controlPoint);

            NodeFindInfo hit = STNodeEditor.FindNodeFromPoint(
                new DrawingPointF(_contextCanvasPoint.X, _contextCanvasPoint.Y));

            _contextMenu.Items.Clear();
            if (hit.Node != null)
            {
                BuildNodeMenu(hit.Node);
            }
            else
            {
                BuildCanvasMenu();
            }
        }

        private void BuildNodeMenu(STNode node)
        {
            _contextMenu.Items.Add(CreateMenuItem("编辑属性", (_, _) => RequestPropertyEditor(node)));
            _contextMenu.Items.Add(new Separator());
            _contextMenu.Items.Add(CreateMenuItem("复制节点", (_, _) => DuplicateNode(node)));
            _contextMenu.Items.Add(CreateMenuItem("删除", (_, _) => STNodeEditor.Nodes.Remove(node)));
            _contextMenu.Items.Add(new Separator());
            _contextMenu.Items.Add(CreateCheckableMenuItem(
                "锁定连接点",
                node.LockOption,
                isChecked => STNodeEditor.ExecuteEditTransaction("Change option lock", () => node.LockOption = isChecked)));
            _contextMenu.Items.Add(CreateCheckableMenuItem(
                "锁定位置",
                node.LockLocation,
                isChecked => STNodeEditor.ExecuteEditTransaction("Change location lock", () => node.LockLocation = isChecked)));
        }

        private void BuildCanvasMenu()
        {
            _contextMenu.Items.Add(CreateAddNodeMenu());
            _contextMenu.Items.Add(new Separator());
            _contextMenu.Items.Add(CreateCommandMenuItem("撤销", ApplicationCommands.Undo));
            _contextMenu.Items.Add(CreateCommandMenuItem("重做", ApplicationCommands.Redo));
            _contextMenu.Items.Add(CreateCommandMenuItem("粘贴", ApplicationCommands.Paste));
            _contextMenu.Items.Add(CreateCommandMenuItem("全选", ApplicationCommands.SelectAll));
            _contextMenu.Items.Add(new Separator());
            _contextMenu.Items.Add(CreateMenuItem("自动布局", (_, _) => AutoArrange()));
            _contextMenu.Items.Add(CreateMenuItem("适应画布", (_, _) => STNodeEditor.FitCanvasToNodes()));
        }

        private MenuItem CreateAddNodeMenu()
        {
            var addMenu = new MenuItem { Header = "添加节点" };
            foreach (IGrouping<string, KeyValuePair<Type, string>> group in _nodeTypes
                .Where(entry => entry.Key.IsSubclassOf(typeof(STNode)) && !entry.Key.IsAbstract)
                .GroupBy(entry => GetCatalogPath(entry.Key, entry.Value))
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
            {
                var categoryMenu = new MenuItem { Header = group.Key };
                foreach (KeyValuePair<Type, string> entry in group.OrderBy(entry => GetNodeTitle(entry.Key), StringComparer.OrdinalIgnoreCase))
                {
                    Type nodeType = entry.Key;
                    categoryMenu.Items.Add(CreateMenuItem(GetNodeTitle(nodeType), (_, _) => AddNode(nodeType)));
                }
                addMenu.Items.Add(categoryMenu);
            }

            if (addMenu.Items.Count == 0)
            {
                addMenu.Items.Add(new MenuItem { Header = "没有可用节点", IsEnabled = false });
            }
            return addMenu;
        }

        public void ShowAddNodeMenu(FrameworkElement placementTarget)
        {
            ArgumentNullException.ThrowIfNull(placementTarget);
            DrawingSize clientSize = STNodeEditor.ClientSize;
            _contextCanvasPoint = STNodeEditor.ControlToCanvas(
                new DrawingPoint(clientSize.Width / 2, clientSize.Height / 2));

            _addNodeMenu.Items.Clear();
            MenuItem addRoot = CreateAddNodeMenu();
            while (addRoot.Items.Count > 0)
            {
                object item = addRoot.Items[0];
                addRoot.Items.RemoveAt(0);
                _addNodeMenu.Items.Add(item);
            }
            _addNodeMenu.PlacementTarget = placementTarget;
            _addNodeMenu.Placement = PlacementMode.Bottom;
            _addNodeMenu.IsOpen = true;
        }

        public void AutoArrange()
        {
            List<STNode> nodes = STNodeEditor.Nodes.Cast<STNode>().ToList();
            if (nodes.Count == 0)
            {
                return;
            }

            var nodeSet = new HashSet<STNode>(nodes);
            var outgoing = nodes.ToDictionary(node => node, _ => new HashSet<STNode>());
            var incomingCount = nodes.ToDictionary(node => node, _ => 0);
            foreach (ConnectionInfo connection in STNodeEditor.GetConnections())
            {
                STNode? source = connection.Output?.Owner;
                STNode? target = connection.Input?.Owner;
                if (source == null || target == null || ReferenceEquals(source, target)
                    || !nodeSet.Contains(source) || !nodeSet.Contains(target)
                    || !outgoing[source].Add(target))
                {
                    continue;
                }
                incomingCount[target]++;
            }

            var rank = nodes.ToDictionary(node => node, _ => 0);
            var queue = new Queue<STNode>(nodes
                .Where(node => incomingCount[node] == 0)
                .OrderBy(node => node.Top)
                .ThenBy(node => node.Left));
            var visited = new HashSet<STNode>();
            while (queue.Count > 0)
            {
                STNode source = queue.Dequeue();
                if (!visited.Add(source))
                {
                    continue;
                }
                foreach (STNode target in outgoing[source])
                {
                    rank[target] = Math.Max(rank[target], rank[source] + 1);
                    incomingCount[target]--;
                    if (incomingCount[target] == 0)
                    {
                        queue.Enqueue(target);
                    }
                }
            }

            int cycleRank = rank.Values.DefaultIfEmpty(0).Max() + 1;
            foreach (STNode node in nodes.Where(node => !visited.Contains(node)).OrderBy(node => node.Left).ThenBy(node => node.Top))
            {
                rank[node] = cycleRank;
            }

            STNodeEditor.ExecuteEditTransaction("Auto layout", () =>
            {
                int x = 60;
                foreach (IGrouping<int, STNode> layer in nodes
                    .GroupBy(node => rank[node])
                    .OrderBy(group => group.Key))
                {
                    int y = 90;
                    int layerWidth = 0;
                    foreach (STNode node in layer.OrderBy(node => node.Top).ThenBy(node => node.Left))
                    {
                        layerWidth = Math.Max(layerWidth, node.Width);
                        if (!node.LockLocation)
                        {
                            node.Left = x;
                            node.Top = y;
                        }
                        y += node.Height + 42;
                    }
                    x += layerWidth + 120;
                }
            });
            STNodeEditor.FitCanvasToNodes();
        }

        private void RequestPropertyEditor(STNode node)
        {
            SelectOnly(node);
            PropertyEditorRequested?.Invoke(this, EventArgs.Empty);
        }

        private void DuplicateNode(STNode node)
        {
            byte[] data = STNodeEditor.GetNodesData(new[] { node });
            STNodeEditor.ImportSelectionData(data, new DrawingPoint(node.Left + 20, node.Top + 20));
        }

        private void AddNode(Type nodeType)
        {
            if (Activator.CreateInstance(nodeType) is not STNode node)
            {
                return;
            }

            node.Left = _contextCanvasPoint.X;
            node.Top = _contextCanvasPoint.Y;
            STNodeEditor.Nodes.Add(node);
            SelectOnly(node);
        }

        private void SelectOnly(STNode node)
        {
            foreach (STNode selectedNode in STNodeEditor.GetSelectedNode())
            {
                if (!ReferenceEquals(selectedNode, node))
                {
                    STNodeEditor.RemoveSelectedNode(selectedNode);
                }
            }

            STNodeEditor.AddSelectedNode(node);
            STNodeEditor.SetActiveNode(node);
        }

        private static string GetCatalogPath(Type nodeType, string path)
        {
            string prefix = (nodeType.Assembly.GetName().Name ?? string.Empty) + "/";
            string relativePath = path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? path.Substring(prefix.Length)
                : path;
            relativePath = relativePath.Trim('/', '\\');
            return string.IsNullOrWhiteSpace(relativePath) ? "General" : relativePath;
        }

        private static string GetNodeTitle(Type nodeType)
        {
            try
            {
                if (Activator.CreateInstance(nodeType) is STNode node)
                {
                    return !string.IsNullOrWhiteSpace(node.Title) ? node.Title : nodeType.Name;
                }
                return nodeType.Name;
            }
            catch
            {
                return nodeType.Name;
            }
        }

        private MenuItem CreateCommandMenuItem(string header, RoutedUICommand command)
        {
            return new MenuItem
            {
                Header = header,
                Command = command,
                CommandTarget = STNodeEditor
            };
        }

        private static MenuItem CreateMenuItem(string header, RoutedEventHandler click)
        {
            var item = new MenuItem { Header = header };
            item.Click += click;
            return item;
        }

        private static MenuItem CreateCheckableMenuItem(string header, bool isChecked, Action<bool> changed)
        {
            var item = new MenuItem
            {
                Header = header,
                IsCheckable = true,
                IsChecked = isChecked
            };
            item.Click += (_, _) => changed(item.IsChecked);
            return item;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            STNodeEditor.CanvasScaled -= OnCanvasScaled;
            _contextMenu.Opened -= OnContextMenuOpened;
            _addNodeMenu.IsOpen = false;
            if (ReferenceEquals(STNodeEditor.ContextMenu, _contextMenu))
            {
                STNodeEditor.ContextMenu = null;
            }
        }
    }
}
