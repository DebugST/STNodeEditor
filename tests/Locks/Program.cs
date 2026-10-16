using System;
using System.Drawing;
using System.Reflection;
using ST.Library.UI.NodeEditor;

public class LockNode : STNode
{
    public STNodeOption Input { get { return InputOptions[0]; } }
    public STNodeOption Output { get { return OutputOptions[0]; } }
    protected override void OnCreate() {
        InputOptions.Add("In", typeof(int), false);
        OutputOptions.Add("Out", typeof(int), false);
    }
}
static class Program
{
    static void Check(bool condition, string message) {
        if (!condition) throw new Exception(message);
    }
    static STNodeEditor Editor() {
        var editor = new STNodeEditor();
        editor.LoadAssembly(Assembly.GetExecutingAssembly().Location);
        return editor;
    }
    [STAThread]
    static void Main() {
        foreach (bool optionLock in new[] { false, true }) {
            foreach (bool locationLock in new[] { false, true }) {
                using (var editor = Editor())
                using (var restored = Editor()) {
                    var source = new LockNode { Left = 123, Top = 77 };
                    var sink = new LockNode();
                    editor.Nodes.Add(source); editor.Nodes.Add(sink);
                    Check(source.Output.ConnectOption(sink.Input) == ConnectionStatus.Connected, "connect");
                    source.LockOption = sink.LockOption = optionLock;
                    source.LockLocation = sink.LockLocation = locationLock;
                    // Populate the legacy drawing cache; this test isolates locks from repaint timing.
                    using (var image = editor.GetCanvasImage(new Rectangle(0, 0, 400, 300))) { }
                    restored.LoadCanvas(editor.GetCanvasData());
                    var copy = (LockNode)restored.Nodes[0];
                    Check(copy.LockOption == optionLock, "option lock " + optionLock + "/" + locationLock);
                    Check(copy.LockLocation == locationLock, "location lock");
                    Check(copy.Left == 123 && copy.Top == 77, "position");
                    Check(copy.Output.ConnectionCount == 1, "connection restored with locks");
                    Check(restored.Nodes[1].LockOption == optionLock, "target option lock");
                }
            }
        }
        Console.WriteLine("PASS all option/location lock combinations and existing connections");
    }
}
