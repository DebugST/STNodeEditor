namespace ST.Library.UI.NodeEditor;

public class STNodeEditorOptionEventArgs : STNodeOptionEventArgs
{
	private STNodeOption _CurrentOption;

	private bool _Continue = true;

	public STNodeOption CurrentOption => _CurrentOption;

	public bool Continue
	{
		get
		{
			return _Continue;
		}
		set
		{
			_Continue = value;
		}
	}

	public STNodeEditorOptionEventArgs(STNodeOption opTarget, STNodeOption opCurrent, ConnectionStatus cr)
		: base(isSponsor: false, opTarget, cr)
	{
		_CurrentOption = opCurrent;
	}
}
