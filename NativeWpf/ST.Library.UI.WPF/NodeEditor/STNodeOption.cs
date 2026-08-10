using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ST.Library.UI.NodeEditor;

public class STNodeOption
{
	public static readonly STNodeOption Empty = new STNodeOption();

	private STNode _Owner;

	private bool _IsSingle;

	private bool _IsInput;

	private Color _TextColor = Color.White;

	private Color _DotColor = Color.Transparent;

	private string _Text;

	private int _DotLeft;

	private int _DotTop;

	private int _DotSize;

	private Rectangle _TextRectangle;

	private object _Data;

	private Type _DataType;

	protected HashSet<STNodeOption> m_hs_connected;

	public STNode Owner
	{
		get
		{
			return _Owner;
		}
		internal set
		{
			if (value != _Owner)
			{
				if (_Owner != null)
				{
					DisConnectionAll();
				}
				_Owner = value;
			}
		}
	}

	public bool IsSingle => _IsSingle;

	public bool IsInput
	{
		get
		{
			return _IsInput;
		}
		internal set
		{
			_IsInput = value;
		}
	}

	public Color TextColor
	{
		get
		{
			return _TextColor;
		}
		internal set
		{
			if (!(value == _TextColor))
			{
				_TextColor = value;
				Invalidate();
			}
		}
	}

	public Color DotColor
	{
		get
		{
			return _DotColor;
		}
		internal set
		{
			if (!(value == _DotColor))
			{
				_DotColor = value;
				Invalidate();
			}
		}
	}

	public string Text
	{
		get
		{
			return _Text;
		}
		internal set
		{
			if (!(value == _Text))
			{
				_Text = value;
				if (_Owner != null)
				{
					_Owner.BuildSize(bBuildNode: true, bBuildMark: true, bRedraw: true);
				}
			}
		}
	}

	public int DotLeft
	{
		get
		{
			return _DotLeft;
		}
		internal set
		{
			_DotLeft = value;
		}
	}

	public int DotTop
	{
		get
		{
			return _DotTop;
		}
		internal set
		{
			_DotTop = value;
		}
	}

	public int DotSize
	{
		get
		{
			return _DotSize;
		}
		protected set
		{
			_DotSize = value;
		}
	}

	public Rectangle TextRectangle
	{
		get
		{
			return _TextRectangle;
		}
		internal set
		{
			_TextRectangle = value;
		}
	}

	public object Data
	{
		get
		{
			return _Data;
		}
		set
		{
			if (value != null)
			{
				if (_DataType == null)
				{
					return;
				}
				Type type = value.GetType();
				if (type != _DataType && !type.IsSubclassOf(_DataType))
				{
					throw new ArgumentException("无效数据类型 数据类型必须为指定的数据类型或其子类");
				}
			}
			_Data = value;
		}
	}

	public Type DataType
	{
		get
		{
			return _DataType;
		}
		internal set
		{
			_DataType = value;
		}
	}

	public Rectangle DotRectangle => new Rectangle(_DotLeft, _DotTop, _DotSize, _DotSize);

	public int ConnectionCount => m_hs_connected.Count;

	public HashSet<STNodeOption> ConnectedOption => m_hs_connected;

	public event STNodeOptionEventHandler Connected;

	public event STNodeOptionEventHandler Connecting;

	public event STNodeOptionEventHandler DisConnected;

	public event STNodeOptionEventHandler DisConnecting;

	public event STNodeOptionEventHandler DataTransfer;

	private STNodeOption()
	{
		m_hs_connected = new HashSet<STNodeOption>();
	}

	public STNodeOption(string strText, Type dataType, bool bSingle)
	{
		if (dataType == null)
		{
			throw new ArgumentNullException("指定的数据类型不能为空");
		}
		_DotSize = 10;
		m_hs_connected = new HashSet<STNodeOption>();
		_DataType = dataType;
		_Text = strText;
		_IsSingle = bSingle;
	}

	protected void Invalidate()
	{
		if (_Owner != null)
		{
			_Owner.Invalidate();
		}
	}

	protected internal virtual void OnConnected(STNodeOptionEventArgs e)
	{
		if (this.Connected != null)
		{
			this.Connected(this, e);
		}
	}

	protected internal virtual void OnConnecting(STNodeOptionEventArgs e)
	{
		if (this.Connecting != null)
		{
			this.Connecting(this, e);
		}
	}

	protected internal virtual void OnDisConnected(STNodeOptionEventArgs e)
	{
		if (this.DisConnected != null)
		{
			this.DisConnected(this, e);
		}
	}

	protected internal virtual void OnDisConnecting(STNodeOptionEventArgs e)
	{
		if (this.DisConnecting != null)
		{
			this.DisConnecting(this, e);
		}
	}

	protected internal virtual void OnDataTransfer(STNodeOptionEventArgs e)
	{
		if (this.DataTransfer != null)
		{
			this.DataTransfer(this, e);
		}
	}

	protected void STNodeEidtorConnected(STNodeEditorOptionEventArgs e)
	{
		if (_Owner != null && _Owner.Owner != null)
		{
			_Owner.Owner.OnOptionConnected(e);
		}
	}

	protected void STNodeEidtorDisConnected(STNodeEditorOptionEventArgs e)
	{
		if (_Owner != null && _Owner.Owner != null)
		{
			_Owner.Owner.OnOptionDisConnected(e);
		}
	}

	protected virtual bool ConnectingOption(STNodeOption op)
	{
		return ConnectingOptionCore(op, isOwnerOfOwner: true);
	}

	protected virtual bool ConnectingOption(STNodeOption op, bool isOwnerOfOwner)
	{
		return isOwnerOfOwner
			? ConnectingOption(op)
			: ConnectingOptionCore(op, isOwnerOfOwner: false);
	}

	private bool ConnectingOptionCore(STNodeOption op, bool isOwnerOfOwner)
	{
		if (_Owner == null)
		{
			return false;
		}
		if (isOwnerOfOwner && _Owner.Owner == null)
		{
			return false;
		}
		STNodeEditorOptionEventArgs e = new STNodeEditorOptionEventArgs(op, this, ConnectionStatus.Connecting);
		if (isOwnerOfOwner)
		{
			_Owner.Owner.OnOptionConnecting(e);
		}
		OnConnecting(new STNodeOptionEventArgs(isSponsor: true, op, ConnectionStatus.Connecting));
		op.OnConnecting(new STNodeOptionEventArgs(isSponsor: false, this, ConnectionStatus.Connecting));
		return e.Continue;
	}

	protected virtual bool DisConnectingOption(STNodeOption op)
	{
		if (_Owner == null)
		{
			return false;
		}
		if (_Owner.Owner == null)
		{
			return false;
		}
		STNodeEditorOptionEventArgs e = new STNodeEditorOptionEventArgs(op, this, ConnectionStatus.DisConnecting);
		_Owner.Owner.OnOptionDisConnecting(e);
		OnDisConnecting(new STNodeOptionEventArgs(isSponsor: true, op, ConnectionStatus.DisConnecting));
		op.OnDisConnecting(new STNodeOptionEventArgs(isSponsor: false, this, ConnectionStatus.DisConnecting));
		return e.Continue;
	}

	public virtual ConnectionStatus ConnectOption(STNodeOption op)
	{
		return ConnectOptionCore(op, ConnectingOption(op));
	}

	public virtual ConnectionStatus ConnectOption(STNodeOption op, bool isOwnerOfOwner)
	{
		return isOwnerOfOwner
			? ConnectOption(op)
			: ConnectOptionCore(op, ConnectingOption(op, isOwnerOfOwner: false));
	}

	private ConnectionStatus ConnectOptionCore(STNodeOption op, bool continueConnecting)
	{
		if (!continueConnecting)
		{
			STNodeEidtorConnected(new STNodeEditorOptionEventArgs(op, this, ConnectionStatus.Reject));
			return ConnectionStatus.Reject;
		}
		ConnectionStatus connectionStatus = CanConnect(op);
		if (connectionStatus != ConnectionStatus.Connected)
		{
			STNodeEidtorConnected(new STNodeEditorOptionEventArgs(op, this, connectionStatus));
			return connectionStatus;
		}
		connectionStatus = op.CanConnect(this);
		if (connectionStatus != ConnectionStatus.Connected)
		{
			STNodeEidtorConnected(new STNodeEditorOptionEventArgs(op, this, connectionStatus));
			return connectionStatus;
		}
		op.AddConnection(this, bSponsor: false);
		AddConnection(op, bSponsor: true);
		ControlBuildLinePath();
		STNodeEidtorConnected(new STNodeEditorOptionEventArgs(op, this, connectionStatus));
		return connectionStatus;
	}

	public virtual ConnectionStatus CanConnect(STNodeOption op)
	{
		if (this == Empty || op == Empty)
		{
			return ConnectionStatus.EmptyOption;
		}
		if (_IsInput == op.IsInput)
		{
			return ConnectionStatus.SameInputOrOutput;
		}
		if (op.Owner == null || _Owner == null)
		{
			return ConnectionStatus.NoOwner;
		}
		if (op.Owner == _Owner)
		{
			return ConnectionStatus.SameOwner;
		}
		if (_Owner.LockOption || op._Owner.LockOption)
		{
			return ConnectionStatus.Locked;
		}
		if (_IsSingle && m_hs_connected.Count == 1)
		{
			return ConnectionStatus.SingleOption;
		}
		if (op.IsInput && STNodeEditor.CanFindNodePath(op.Owner, _Owner))
		{
			return ConnectionStatus.Loop;
		}
		if (m_hs_connected.Contains(op))
		{
			return ConnectionStatus.Exists;
		}
		if (_IsInput && op._DataType != _DataType && !op._DataType.IsSubclassOf(_DataType))
		{
			return ConnectionStatus.ErrorType;
		}
		return ConnectionStatus.Connected;
	}

	public virtual ConnectionStatus DisConnectOption(STNodeOption op)
	{
		if (!DisConnectingOption(op))
		{
			STNodeEidtorDisConnected(new STNodeEditorOptionEventArgs(op, this, ConnectionStatus.Reject));
			return ConnectionStatus.Reject;
		}
		if (op.Owner == null)
		{
			return ConnectionStatus.NoOwner;
		}
		if (_Owner == null)
		{
			return ConnectionStatus.NoOwner;
		}
		if (op.Owner.LockOption && _Owner.LockOption)
		{
			STNodeEidtorDisConnected(new STNodeEditorOptionEventArgs(op, this, ConnectionStatus.Locked));
			return ConnectionStatus.Locked;
		}
		op.RemoveConnection(this, bSponsor: false);
		RemoveConnection(op, bSponsor: true);
		ControlBuildLinePath();
		STNodeEidtorDisConnected(new STNodeEditorOptionEventArgs(op, this, ConnectionStatus.DisConnected));
		return ConnectionStatus.DisConnected;
	}

	public void DisConnectionAll()
	{
		if (!(_DataType == null))
		{
			STNodeOption[] array = m_hs_connected.ToArray();
			STNodeOption[] array2 = array;
			foreach (STNodeOption op in array2)
			{
				DisConnectOption(op);
			}
		}
	}

	internal void DisconnectAllDetached()
	{
		if (_DataType == null)
		{
			return;
		}
		foreach (STNodeOption option in m_hs_connected.ToArray())
		{
			option.RemoveConnection(this, bSponsor: false);
			RemoveConnection(option, bSponsor: true);
		}
		ControlBuildLinePath();
	}

	public List<STNodeOption> GetConnectedOption()
	{
		if (_DataType == null)
		{
			return null;
		}
		if (!_IsInput)
		{
			return m_hs_connected.ToList();
		}
		List<STNodeOption> list = new List<STNodeOption>();
		if (_Owner == null)
		{
			return null;
		}
		if (_Owner.Owner == null)
		{
			return m_hs_connected.ToList();
		}
		ConnectionInfo[] connectionInfo = _Owner.Owner.GetConnectionInfo();
		for (int i = 0; i < connectionInfo.Length; i++)
		{
			ConnectionInfo connectionInfo2 = connectionInfo[i];
			if (connectionInfo2.Output == this)
			{
				list.Add(connectionInfo2.Input);
			}
		}
		return list;
	}

	public void TransferData()
	{
		if (_DataType == null)
		{
			return;
		}
		foreach (STNodeOption item in m_hs_connected)
		{
			item.OnDataTransfer(new STNodeOptionEventArgs(isSponsor: true, this, ConnectionStatus.Connected));
		}
	}

	public void TransferData(object data)
	{
		if (_DataType == null)
		{
			return;
		}
		Data = data;
		foreach (STNodeOption item in m_hs_connected)
		{
			item.OnDataTransfer(new STNodeOptionEventArgs(isSponsor: true, this, ConnectionStatus.Connected));
		}
	}

	public void TransferData(object data, bool bDisposeOld)
	{
		if (bDisposeOld && _Data != null)
		{
			if (_Data is IDisposable)
			{
				((IDisposable)_Data).Dispose();
			}
			_Data = null;
		}
		TransferData(data);
	}

	private bool AddConnection(STNodeOption op, bool bSponsor)
	{
		if (_DataType == null)
		{
			return false;
		}
		bool result = m_hs_connected.Add(op);
		OnConnected(new STNodeOptionEventArgs(bSponsor, op, ConnectionStatus.Connected));
		if (_IsInput)
		{
			OnDataTransfer(new STNodeOptionEventArgs(bSponsor, op, ConnectionStatus.Connected));
		}
		return result;
	}

	private bool RemoveConnection(STNodeOption op, bool bSponsor)
	{
		if (_DataType == null)
		{
			return false;
		}
		bool result = false;
		if (m_hs_connected.Contains(op))
		{
			result = m_hs_connected.Remove(op);
			if (_IsInput)
			{
				OnDataTransfer(new STNodeOptionEventArgs(bSponsor, op, ConnectionStatus.DisConnected));
			}
			OnDisConnected(new STNodeOptionEventArgs(bSponsor, op, ConnectionStatus.Connected));
		}
		return result;
	}

	private void ControlBuildLinePath()
	{
		if (Owner != null && Owner.Owner != null)
		{
			Owner.Owner.BuildLinePath();
		}
	}
}
