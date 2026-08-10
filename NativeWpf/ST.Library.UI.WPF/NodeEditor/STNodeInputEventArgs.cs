using System;
using System.Drawing;

namespace ST.Library.UI.NodeEditor;

[Flags]
public enum STMouseButtons
{
	None = 0,
	Left = 1,
	Right = 2,
	Middle = 4,
	XButton1 = 8,
	XButton2 = 16
}

public sealed class STNodeMouseEventArgs : EventArgs
{
	public STMouseButtons Button { get; }

	public int Clicks { get; }

	public int X { get; }

	public int Y { get; }

	public int Delta { get; }

	public Point Location => new Point(X, Y);

	public STNodeMouseEventArgs(STMouseButtons button, int clicks, int x, int y, int delta)
	{
		Button = button;
		Clicks = clicks;
		X = x;
		Y = y;
		Delta = delta;
	}

	public STNodeMouseEventArgs WithLocation(int x, int y)
	{
		return new STNodeMouseEventArgs(Button, Clicks, x, y, Delta);
	}
}

public delegate void STNodeMouseEventHandler(object sender, STNodeMouseEventArgs e);

public sealed class STNodeKeyPressEventArgs : EventArgs
{
	public char KeyChar { get; }

	public bool Handled { get; set; }

	public STNodeKeyPressEventArgs(char keyChar)
	{
		KeyChar = keyChar;
	}
}

public delegate void STNodeKeyPressEventHandler(object sender, STNodeKeyPressEventArgs e);
