using System;

namespace ST.Library.UI.NodeEditor;

public class STNodeOptionEventArgs : EventArgs
{
	private STNodeOption _TargetOption;

	private ConnectionStatus _Status;

	private bool _IsSponsor;

	public STNodeOption TargetOption => _TargetOption;

	public ConnectionStatus Status
	{
		get
		{
			return _Status;
		}
		internal set
		{
			_Status = value;
		}
	}

	public bool IsSponsor => _IsSponsor;

	public STNodeOptionEventArgs(bool isSponsor, STNodeOption opTarget, ConnectionStatus cr)
	{
		_IsSponsor = isSponsor;
		_TargetOption = opTarget;
		_Status = cr;
	}
}
