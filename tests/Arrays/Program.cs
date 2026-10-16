using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using ST.Library.UI.NodeEditor;
#if NET8_0_OR_GREATER
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif

public class DoubleArrayNode : STNode
{
    [STNodeProperty("Doubles", "Double array")]
    public double[] Values { get; set; }
}
public class BoolArrayNode : STNode
{
    [STNodeProperty("Booleans", "Boolean array")]
    public bool[] Values { get; set; }
}
static class Program
{
    static STNodeEditor Editor() {
        var editor = new STNodeEditor();
        editor.LoadAssembly(Assembly.GetExecutingAssembly().Location);
        return editor;
    }
    static STNode Roundtrip(STNode node) {
        var editor = Editor();
        editor.Nodes.Add(node);
        var restored = Editor();
        restored.LoadCanvas(editor.GetCanvasData());
        return restored.Nodes[0];
    }
    [STAThread]
    static void Main() {
        System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        double[] doubles = { 1.25, -2.5, 0.0 };
        bool[] booleans = { true, false, true };
        var doubleCopy = (DoubleArrayNode)Roundtrip(new DoubleArrayNode { Values = doubles });
        if (!doubleCopy.Values.SequenceEqual(doubles)) throw new Exception("double[] roundtrip");
        var boolCopy = (BoolArrayNode)Roundtrip(new BoolArrayNode { Values = booleans });
        if (!boolCopy.Values.SequenceEqual(booleans)) throw new Exception("bool[] roundtrip");
        Console.WriteLine("PASS double[] and bool[] property canvas roundtrips");
    }
}
