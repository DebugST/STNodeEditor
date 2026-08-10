using System;

namespace ST.Library.UI.NodeEditor;

public class STNodeEditorEventArgs : EventArgs
{
	private STNode _Node;

	public STNode Node => _Node;

	public STNodeEditorEventArgs(STNode node)
	{
		_Node = node;
	}
}
