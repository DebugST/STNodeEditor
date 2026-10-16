using System;
using System.Reflection;
using ST.Library.UI.NodeEditor;
#if NET8_0_OR_GREATER
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif

public class ConnectionNode : STNode
{
    public STNodeOption Input { get { return InputOptions[0]; } }
    public STNodeOption Output { get { return OutputOptions[0]; } }
    protected override void OnCreate() {
        InputOptions.Add("In", typeof(int), false);
        OutputOptions.Add("Out", typeof(int), false);
        OutputOptions.Add(STNodeOption.Empty);
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
        var editor = Editor();
        var source = new ConnectionNode();
        var other = new ConnectionNode();
        var sink = new ConnectionNode();
        editor.Nodes.Add(source); editor.Nodes.Add(other); editor.Nodes.Add(sink);
        int received = 0;
        sink.Input.DataTransfer += (s, e) => {
            if (e.Status == ConnectionStatus.Connected && e.TargetOption.Data != null)
                received = (int)e.TargetOption.Data;
        };
        Check(source.Output.ConnectOption(sink.Input) == ConnectionStatus.Connected, "connect");
        source.Output.TransferData(42);
        Check(received == 42, "data transfer");
        Check(editor.GetConnectionInfo().Length == 1, "connection info before paint");
        Check(source.Output.GetConnectedOption().Contains(sink.Input), "output peer");
        Check(sink.Input.ConnectionCount == 1, "input count");
        Check(sink.Input.GetConnectedOption().Count == 1 && sink.Input.GetConnectedOption()[0] == source.Output, "input peer");
        var restored = Editor();
        restored.LoadCanvas(editor.GetCanvasData());
        Check(((ConnectionNode)restored.Nodes[0]).Output.ConnectionCount == 1, "roundtrip before paint");
        Check(other.Output.ConnectOption(sink.Input) == ConnectionStatus.Connected, "second source");
        Check(sink.Input.GetConnectedOption().Count == 2, "multiple input peers");
        source.Output.DisConnectOption(sink.Input);
        Check(sink.Input.GetConnectedOption().Count == 1 && sink.Input.GetConnectedOption()[0] == other.Output, "remaining peer");
        Check(editor.GetConnectionInfo().Length == 1, "connection info after disconnect");
        other.Output.DisConnectOption(sink.Input);
        Check(sink.Input.GetConnectedOption().Count == 0, "disconnected peers");
        Check(editor.GetConnectionInfo().Length == 0, "empty connection info");
        Console.WriteLine("PASS connection queries, transfer and canvas roundtrip without painting");
    }
}
