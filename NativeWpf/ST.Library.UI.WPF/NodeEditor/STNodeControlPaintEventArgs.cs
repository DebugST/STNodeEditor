using System;

namespace ST.Library.UI.NodeEditor;

public class STNodeControlPaintEventArgs : EventArgs
{
	public DrawingTools DrawingTools { get; private set; }

	public STNodeControlPaintEventArgs(DrawingTools dt)
	{
		DrawingTools = dt;
	}
}
