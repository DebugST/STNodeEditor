#pragma warning disable CS8603,CS8604


using ST.Library.UI.NodeEditor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace ColorVision.Engine.Templates.Flow
{

    public class STNodeEditorHelper
    {
        public STNodeEditor STNodeEditor { get; set; }

        public STNodePropertyGrid STNodePropertyGrid1 { get; set; }

        public STNodeTreeView STNodeTreeView1 { get; set; }

        public STNodeEditorHelper(Control Paraent,STNodeEditor sTNodeEditor, STNodeTreeView sTNodeTreeView1, STNodePropertyGrid sTNodePropertyGrid)
        {
            STNodeEditor = sTNodeEditor;
            STNodeTreeView1 = sTNodeTreeView1;
            STNodePropertyGrid1 = sTNodePropertyGrid;
            STNodeEditor.NodeAdded += StNodeEditor1_NodeAdded;
            STNodeEditor.ActiveChanged += STNodeEditorMain_ActiveChanged;

            AddContentMenu();

            Paraent.CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, (s, e) => 
            {
                foreach (var item in STNodeEditor.GetSelectedNode())
                    STNodeEditor.Nodes.Remove(item);
            } , (s, e) => { e.CanExecute = sTNodeEditor.GetSelectedNode().Length > 0; }));


            Paraent.CommandBindings.Add(new CommandBinding(ApplicationCommands.New, (s, e) => sTNodeEditor.Nodes.Clear(), (s, e) => { e.CanExecute = true; }));

            Paraent.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, (s, e) => Copy(), (s, e) => { e.CanExecute = true; }));
            Paraent.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, (s, e) => Paste(), (s, e) => { e.CanExecute = CopyNodes.Count >0;}));
            Paraent.CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, (s, e) => SelectAll(), (s, e) => { e.CanExecute = true; }));

            Paraent.CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, (s, e) => sTNodeEditor.Nodes.Clear(), (s, e) => { e.CanExecute = true; }));
        }

        private List<STNode> CopyNodes = new List<STNode>();

        public void SelectAll()
        {
            foreach (var item in STNodeEditor.Nodes.OfType<STNode>())
            {
                STNodeEditor.AddSelectedNode(item);
            }
        }

        public void Copy()
        {
            CopyNodes.Clear();
            foreach (var item in STNodeEditor.GetSelectedNode())
            {
                CopyNodes.Add(item);
            }
        }

        public void Paste()
        {
            int offset = 10;

            foreach (var item in CopyNodes)
            {
                Type type = item.GetType();

                STNode sTNode1 = (STNode)Activator.CreateInstance(type);
                if (sTNode1 != null)
                {
                    PropertyInfo[] properties = type.GetProperties();
                    foreach (PropertyInfo property in properties)
                    {
                        if (property.CanRead && property.CanWrite)
                        {
                            object value = property.GetValue(item);
                            property.SetValue(sTNode1, value);
                        }
                    }
                    sTNode1.Left = item.Left + offset;
                    sTNode1.Top = item.Top + offset;
                    sTNode1.IsSelected = true;
                    STNodeEditor.Nodes.Add(sTNode1);
                    if (CopyNodes.Count == 1)
                    {
                        item.IsSelected = false;
                        STNodeEditor.RemoveSelectedNode(item);
                        STNodeEditor.AddSelectedNode(sTNode1);
                        STNodeEditor.SetActiveNode(sTNode1);
                    }
                    else
                    {
                        STNodeEditor.RemoveSelectedNode(item);
                        STNodeEditor.AddSelectedNode(sTNode1);
                    }
                }
            }

            CopyNodes.Clear();
            foreach (var item in STNodeEditor.GetSelectedNode())
            {
                CopyNodes.Add(item);
            }

        }



        #region Activate
        private void STNodeEditorMain_ActiveChanged(object? sender, EventArgs e)
        {
            STNodePropertyGrid1.SetNode(STNodeEditor.ActiveNode);
        }
        #endregion

        #region ContextMenu

        public void AddNodeContext()
        {
            foreach (var item in STNodeEditor.Nodes)
            {
                if (item is STNode node)
                {
                    node.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
                    node.ContextMenuStrip.Items.Add("复制", null, (s, e1) => CopySTNode(node));
                    node.ContextMenuStrip.Items.Add("删除", null, (s, e1) => STNodeEditor.Nodes.Remove(node));
                    node.ContextMenuStrip.Items.Add("LockOption", null, (s, e1) => STNodeEditor.ActiveNode.LockOption = !STNodeEditor.ActiveNode.LockOption);
                    node.ContextMenuStrip.Items.Add("LockLocation", null, (s, e1) => STNodeEditor.ActiveNode.LockLocation = !STNodeEditor.ActiveNode.LockLocation);
                }
            }
        }


        private void StNodeEditor1_NodeAdded(object sender, STNodeEditorEventArgs e)
        {
            STNode node = e.Node;
            node.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            node.ContextMenuStrip.Items.Add("删除", null, (s, e1) => STNodeEditor.Nodes.Remove(node));
            node.ContextMenuStrip.Items.Add("复制", null, (s, e1) => CopySTNode(node));
            node.ContextMenuStrip.Items.Add("LockOption", null, (s, e1) => STNodeEditor.ActiveNode.LockOption = !STNodeEditor.ActiveNode.LockOption);
            node.ContextMenuStrip.Items.Add("LockLocation", null, (s, e1) => STNodeEditor.ActiveNode.LockLocation = !STNodeEditor.ActiveNode.LockLocation);
        }

        public void CopySTNode(STNode sTNode)
        {
            Type type = sTNode.GetType();

            STNode sTNode1 = (STNode)Activator.CreateInstance(type);
            if (sTNode1 != null)
            {
                PropertyInfo[] properties = type.GetProperties();
                foreach (PropertyInfo property in properties)
                {
                    if (property.CanRead && property.CanWrite)
                    {
                        object value = property.GetValue(sTNode);
                        property.SetValue(sTNode1, value);
                    }
                }
                sTNode1.Left = sTNode.Left;
                sTNode1.Top = sTNode.Top;

                STNodeEditor.Nodes.Add(sTNode1);
            }
        }

        public void AddContentMenu()
        {
            STNodeEditor.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            Type STNodeTreeViewtype = STNodeTreeView1.GetType();

            // 获取私有字段信息
            FieldInfo fieldInfo = STNodeTreeViewtype.GetField("m_dic_all_type", BindingFlags.NonPublic | BindingFlags.Instance);

            if (fieldInfo != null)
            {
                // 获取字段的值
                var value = fieldInfo.GetValue(STNodeTreeView1);
                Dictionary<string, List<Type>> values = new Dictionary<string, List<Type>>();
                if (value is Dictionary<Type, string> m_dic_all_type)
                {
                    foreach (var item in m_dic_all_type)
                    {
                        if (values.TryGetValue(item.Value, out List<Type>? value1))
                        {
                            value1.Add(item.Key);
                        }
                        else
                        {
                            values.Add(item.Value, new List<Type>() { item.Key });
                        }
                    }

                    foreach (var nodetype in values.OrderBy(x => x.Key, Comparer<string>.Create((x, y) =>string.Compare(x,y))))
                    {
                        string header = nodetype.Key.Replace("WpfNodeEdittorDemo/", "");
                        var toolStripItem = new System.Windows.Forms.ToolStripMenuItem(header);


                        foreach (var type in nodetype.Value)
                        {
                            if (type.IsSubclassOf(typeof(STNode)))
                            {
                                if (Activator.CreateInstance(type) is STNode sTNode)
                                {
                                    toolStripItem.DropDownItems.Add(sTNode.Title, null, (s, e) =>
                                    {
                                        STNode sTNode1 = (STNode)Activator.CreateInstance(type);
                                        if (sTNode1 != null)
                                        {
                                            var p = STNodeEditor.PointToClient(lastMousePosition);
                                            p = STNodeEditor.ControlToCanvas(p);
                                            sTNode1.Left = p.X;
                                            sTNode1.Top = p.Y;
                                            STNodeEditor.Nodes.Add(sTNode1);
                                        }
                                    });
                                }
                            }

                        }
                        STNodeEditor.ContextMenuStrip.Items.Add(toolStripItem);

                    }

                }
            }


            STNodeEditor.ContextMenuStrip.Opening += (s, e) =>
            {
                if (IsOptionDisConnected) e.Cancel = true;
                if (IsHover())
                    e.Cancel = true;
                IsOptionDisConnected = false;
            };
            STNodeEditor.OptionDisConnected += (s, e) =>
            {
                IsOptionDisConnected = true;
            };
        }
        bool IsOptionDisConnected;


        private System.Drawing.Point lastMousePosition;

        public bool IsHover()
        {
            lastMousePosition = System.Windows.Forms.Cursor.Position;
            var p = STNodeEditor.PointToClient(System.Windows.Forms.Cursor.Position);
            p = STNodeEditor.ControlToCanvas(p);

            foreach (var item in STNodeEditor.Nodes)
            {
                if (item is STNode sTNode)
                {
                    bool result = sTNode.Rectangle.Contains(p);
                    if (result)
                        return true;

                    if (sTNode.GetInputOptions() is STNodeOption[] inputOptions)
                    {
                        foreach (STNodeOption inputOption in inputOptions)
                        {
                            if (inputOption != STNodeOption.Empty && inputOption.DotRectangle.Contains(p))
                            {
                                return true;
                            }
                        }
                    }

                    if (sTNode.GetOutputOptions() is STNodeOption[] outputOptions)
                    {
                        foreach (STNodeOption outputOption in outputOptions)
                        {
                            if (outputOption != STNodeOption.Empty && outputOption.DotRectangle.Contains(p))
                            {
                                return true;
                            }
                        }

                    }
                }
            }
            return false;
        }

        #endregion

        #region AutoLayout
        public ConnectionInfo[] ConnectionInfo { get; set; }
        public float CanvasScale { get => STNodeEditor.CanvasScale; set { STNodeEditor.ScaleCanvas(value, STNodeEditor.CanvasValidBounds.X + STNodeEditor.CanvasValidBounds.Width / 2, STNodeEditor.CanvasValidBounds.Y + STNodeEditor.CanvasValidBounds.Height / 2);  } }
        public void AutoSize()
        {
            // Calculate the centers
            var boundsCenterX = STNodeEditor.Bounds.Width / 2;
            var boundsCenterY = STNodeEditor.Bounds.Height / 2;

            // Calculate the scale factor to fit CanvasValidBounds within Bounds
            var scaleX = (float)STNodeEditor.Bounds.Width / (float)STNodeEditor.CanvasValidBounds.Width;
            var scaleY = (float)STNodeEditor.Bounds.Height / (float)STNodeEditor.CanvasValidBounds.Height;
            CanvasScale = Math.Min(scaleX, scaleY);
            CanvasScale = CanvasScale > 1 ? 1 : CanvasScale;
            // Apply the scale
            STNodeEditor.ScaleCanvas(CanvasScale, STNodeEditor.CanvasValidBounds.X + STNodeEditor.CanvasValidBounds.Width / 2, STNodeEditor.CanvasValidBounds.Y + STNodeEditor.CanvasValidBounds.Height / 2);

            var validBoundsCenterX = STNodeEditor.CanvasValidBounds.Width / 2;
            var validBoundsCenterY = STNodeEditor.CanvasValidBounds.Height / 2;

            // Calculate the offsets to move CanvasValidBounds to the center of Bounds
            var offsetX = boundsCenterX - validBoundsCenterX * CanvasScale - 50 * CanvasScale;
            var offsetY = boundsCenterY - validBoundsCenterY * CanvasScale - 50 * CanvasScale;


            // Move the canvas
            STNodeEditor.MoveCanvas(offsetX, STNodeEditor.CanvasOffset.Y, bAnimation: true, CanvasMoveArgs.Left);
            STNodeEditor.MoveCanvas(offsetX, offsetY, bAnimation: true, CanvasMoveArgs.Top);
        }

        public void ApplyTreeLayout(int startX, int startY, int horizontalSpacing, int verticalSpacing)
        {
            ConnectionInfo = STNodeEditor.GetConnectionInfo();
            STNode rootNode = null;
            if (rootNode == null) return;
            int currentY = startY;
            HashSet<STNode> MoreParens = new HashSet<STNode>();

            void LayoutNode(STNode node, int current)
            {
                int depeth = GetMaxDepth(node);
                // 设置当前节点的位置
                node.Left = startX + depeth * horizontalSpacing;
                node.Top = current;

                var parent = GetParent(node);
                // 递归布局子节点
                var children = GetChildren(node);

                foreach (var child in children)
                {
                    if (GetParent(child).Count > 1)
                    {
                        MoreParens.Add(child);
                    }
                    else
                    {
                        LayoutNode(child, currentY);
                        var childrenWithout1 = GetChildrenWithout(node);
                        if (childrenWithout1.Count > 1)
                        {
                            currentY += verticalSpacing;
                        }
                    }
                }
                var childrenWithout = GetChildrenWithout(node);
                if (childrenWithout.Count > 1)
                {
                    currentY = childrenWithout.Last().Top;
                }

                // 调整父节点位置到子节点的中心
                if (childrenWithout.Count != 0)
                {
                    int firstChildY = childrenWithout.First().Top;
                    int lastChildY = childrenWithout.Last().Top;
                    node.Top = (firstChildY + lastChildY) / 2;
                }

                if (parent.Count > 1)
                {
                    int firstChildY = parent.First().Top;
                    int lastChildY = parent.Last().Top;
                    node.Top = (firstChildY + lastChildY) / 2;
                }
            }

            void MoreParentsLayoutNode(STNode node)
            {
                node.Left = startX + GetMaxDepth(node) * horizontalSpacing;
                var parent = GetParent(node);
                // 递归布局子节点
                var children = GetChildren(node);

                int minParentY = parent.Min(c => c.Top);
                int maxParentY = parent.Max(c => c.Top);

                node.Top = (minParentY + maxParentY) / 2;

                SetCof(node, verticalSpacing);
                int currenty = node.Top;
                foreach (var child in children)
                {
                    LayoutNode(child, currenty);
                    currenty += verticalSpacing;
                }
                MoreParens.Remove(node);
            }
            LayoutNode(rootNode, currentY);
            while (MoreParens.Count > 0)
            {
                foreach (var item in MoreParens.Cast<STNode>().ToList())
                {
                    MoreParentsLayoutNode(item);
                }
            }

        }

        public void SetCof(STNode node, int verticalSpacing)
        {
            foreach (var item in STNodeEditor.Nodes)
            {
                if (item is STNode onode)
                {
                    if (onode != node && onode.Left == node.Left && onode.Top == node.Top)
                    {
                        onode.Top += verticalSpacing;
                        SetCof(node, verticalSpacing);
                    }
                }
            }
        }


        public int GetMaxDepth(STNode node)
        {
            var parent = GetParent(node);
            if (parent.Count == 0)
            {
                return 0;
            }
            return parent.Max(c => GetMaxDepth(c)) + 1;
        }

        List<STNode> GetParent(STNode node)
        {
            var list = ConnectionInfo.Where(c => c.Input.Owner == node);
            List<STNode> children = new();
            foreach (var item in list)
            {
                children.Add(item.Output.Owner);

            }
            return children;
        }
        List<STNode> GetChildrenWithout(STNode node)
        {
            var list = ConnectionInfo.Where(c => c.Output.Owner == node);
            List<STNode> children = new();
            foreach (var item in list)
            {
                if (GetParent(item.Input.Owner).Count == 1)
                {
                    children.Add(item.Input.Owner);
                }
            }
            return children;
        }

        List<STNode> GetChildren(STNode node)
        {
            var list = ConnectionInfo.Where(c => c.Output.Owner == node);
            List<STNode> children = new();
            foreach (var item in list)
            {
                children.Add(item.Input.Owner);

            }
            return children;
        }
        private bool IsPathExists(STNode startNode, STNode endNode)
        {
            var visited = new HashSet<STNode>();
            var queue = new Queue<STNode>();
            queue.Enqueue(startNode);

            while (queue.Count > 0)
            {
                var currentNode = queue.Dequeue();
                if (currentNode == endNode)
                {
                    return true;
                }

                visited.Add(currentNode);

                var children = GetChildren(currentNode);
                foreach (var child in children)
                {
                    if (!visited.Contains(child))
                    {
                        queue.Enqueue(child);
                    }
                }
            }

            return false;
        }
        #endregion
    }
}
