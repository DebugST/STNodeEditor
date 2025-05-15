
using ColorVision.Engine.Templates.Flow;
using ST.Library.UI.NodeEditor;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace WpfNodeEdittorDemo
{
    public class ActionCommand
    {
        public string Header { get; set; }

        public Action UndoAction { get; set; }
        public Action RedoAction { get; set; }

        public ActionCommand(Action undoAction, Action redoAction)
        {
            UndoAction = undoAction;
            RedoAction = redoAction;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Undo, (s, e) => Undo(), (s, e) => { e.CanExecute = UndoStack.Count > 0; }));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Redo, (s, e) => Redo(), (s, e) => { e.CanExecute = RedoStack.Count > 0; }));
        }
        #region ActionCommand

        public ObservableCollection<ActionCommand> UndoStack { get; set; } = new ObservableCollection<ActionCommand>();
        public ObservableCollection<ActionCommand> RedoStack { get; set; } = new ObservableCollection<ActionCommand>();

        public void ClearActionCommand()
        {
            UndoStack.Clear();
            RedoStack.Clear();
        }

        public void AddActionCommand(ActionCommand actionCommand)
        {
            UndoStack.Add(actionCommand);
            RedoStack.Clear();
        }

        public void Undo()
        {
            if (UndoStack.Count > 0)
            {
                var undoAction = UndoStack[^1]; // Access the last element
                UndoStack.RemoveAt(UndoStack.Count - 1); // Remove the last element
                undoAction.UndoAction();
                RedoStack.Add(undoAction);
            }
        }

        public void Redo()
        {
            if (RedoStack.Count > 0)
            {
                var redoAction = RedoStack[^1]; // Access the last element
                RedoStack.RemoveAt(RedoStack.Count - 1); // Remove the last element
                redoAction.RedoAction();
                UndoStack.Add(redoAction);
            }
        }
        #endregion


        private void Window_Initialized(object sender, EventArgs e)
        {
            STNodePropertyGrid1.Text = "Node_Property";
            STNodeTreeView1.LoadAssembly( System.Windows.Forms.Application.ExecutablePath.Replace("exe","dll"));
            STNodeEditorMain.LoadAssembly(System.Windows.Forms.Application.ExecutablePath.Replace("exe", "dll"));

            STNodeEditorHelper STNodeEditorHelper = new STNodeEditorHelper(this, STNodeEditorMain, STNodeTreeView1, STNodePropertyGrid1);

        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void Button_Click_Open(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new();
            ofd.Filter = "*.stn|*.stn";
            ofd.RestoreDirectory = true;
            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            ButtonSave.Visibility = Visibility.Visible;
            OpenFlow(ofd.FileName);
        }
        string FileFlow;
        public void OpenFlow(string flowName)
        {
            FileFlow = flowName;
            STNodeEditorMain.Nodes.Clear();
            STNodeEditorMain.LoadCanvas(flowName);
            Title = "流程编辑器 - " + new FileInfo(flowName).Name;
        }
        private bool IsMouseDown;
        private System.Drawing.Point lastMousePosition;

        private void STNodeEditorMain_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            lastMousePosition = e.Location;
            System.Drawing.PointF m_pt_down_in_canvas = new System.Drawing.PointF();
            m_pt_down_in_canvas.X = ((float)e.X - STNodeEditorMain.CanvasOffsetX) / STNodeEditorMain.CanvasScale;
            m_pt_down_in_canvas.Y = ((float)e.Y - STNodeEditorMain.CanvasOffsetY) / STNodeEditorMain.CanvasScale;
            NodeFindInfo nodeFindInfo = STNodeEditorMain.FindNodeFromPoint(m_pt_down_in_canvas);

            if (!string.IsNullOrEmpty(nodeFindInfo.Mark))
            {

            }
            else if (nodeFindInfo.Node != null)
            {

            }
            else if (nodeFindInfo.NodeOption != null)
            {

            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                IsMouseDown = true;
            }
        }

        private void STNodeEditorMain_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            IsMouseDown = false;
        }

        private void STNodeEditorMain_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && IsMouseDown)
            {        // 计算鼠标移动的距离
                int deltaX = e.X - lastMousePosition.X;
                int deltaY = e.Y - lastMousePosition.Y;

                // 更新画布偏移
                STNodeEditorMain.MoveCanvas(
                    STNodeEditorMain.CanvasOffsetX + deltaX,
                    STNodeEditorMain.CanvasOffsetY + deltaY,
                    bAnimation: false,
                    CanvasMoveArgs.All
                );

                // 更新最后的鼠标位置
                lastMousePosition = e.Location;
            }
        }

        private void STNodeEditorMain_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            var mousePosition = STNodeEditorMain.PointToClient(e.Location);

            if (e.Delta < 0)
            {
                STNodeEditorMain.ScaleCanvas(STNodeEditorMain.CanvasScale - 0.05f, mousePosition.X, mousePosition.Y);
            }
            else
            {
                STNodeEditorMain.ScaleCanvas(STNodeEditorMain.CanvasScale + 0.05f, mousePosition.X, mousePosition.Y);
            }
        }

        private void Button_Click_Clear(object sender, RoutedEventArgs e)
        {
            FileFlow = string.Empty;
            STNodeEditorMain.Nodes.Clear();
        }

        private void Button_Click_Save(object sender, RoutedEventArgs e)
        {
            Save();
        }
        private void Save()
        {
            if (string.IsNullOrEmpty(FileFlow) || !File.Exists(FileFlow))
            {
                using (System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog())
                {
                    saveFileDialog.Filter = "*.stn|*.stn";
                    saveFileDialog.Title = "Select a File to Save";

                    if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        FileFlow = saveFileDialog.FileName;
                    }
                    else
                    {
                        // User cancelled the dialog
                        return;
                    }
                }
            }

            SaveToFile(FileFlow);
            MessageBox.Show("保存成功");
        }

        public void SaveToFile(string filePath)
        {
            // 获取画布数据
            byte[] data = STNodeEditorMain.GetCanvasData();

            // 检查数据是否为空
            if (data == null || data.Length == 0)
            {
                Console.WriteLine("No data to save.");
                return;
            }

            try
            {
                // 创建文件路径的目录（如果不存在）
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 将数据写入指定文件路径
                File.WriteAllBytes(filePath, data);
                Console.WriteLine("File saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while saving the file: {ex.Message}");
            }
        }


        private void Button_Click_New(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new();
            ofd.Filter = "*.stn|*.stn";
            ofd.RestoreDirectory = true;
            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            OpenFlow(ofd.FileName);

        }

        private void AutoAlignment_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}