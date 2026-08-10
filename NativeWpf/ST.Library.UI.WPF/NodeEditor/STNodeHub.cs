using System;
using System.Collections.Generic;
using System.Drawing;

namespace ST.Library.UI.NodeEditor;

public class STNodeHub : STNode
{
	private bool m_bSingle;

	private string m_strIn;

	private string m_strOut;

	public STNodeHub()
		: this(bSingle: false, "IN", "OUT", "HUB")
	{
	}

	public STNodeHub(bool bSingle)
		: this(bSingle, "IN", "OUT", "HUB")
	{
	}

	public STNodeHub(bool bSingle, string title)
		: this(bSingle, "IN", "OUT", title)
	{
	}

	public STNodeHub(bool bSingle, string strTextIn, string strTextOut)
		: this(bSingle, strTextIn, strTextOut, "HUB")
	{
	}

	public STNodeHub(bool bSingle, string strTextIn, string strTextOut, string title)
	{
		m_bSingle = bSingle;
		m_strIn = strTextIn;
		m_strOut = strTextOut;
		Addhub();
		base.Title = Lang.Get(title);
		base.AutoSize = false;
		base.TitleColor = Color.FromArgb(200, Color.DarkOrange);
	}

	protected override void OnOwnerChanged()
	{
		base.OnOwnerChanged();
		if (base.Owner == null)
		{
			return;
		}
		base.Width = base.GetDefaultNodeSize().Width;
	}

	private void Addhub()
	{
		STNodeHubOption sTNodeHubOption = new STNodeHubOption(m_strIn, typeof(object), m_bSingle);
		STNodeHubOption sTNodeHubOption2 = new STNodeHubOption(m_strOut, typeof(object), bSingle: false);
		base.InputOptions.Add(sTNodeHubOption);
		base.OutputOptions.Add(sTNodeHubOption2);
		sTNodeHubOption.Connected += input_Connected;
		sTNodeHubOption.DataTransfer += input_DataTransfer;
		sTNodeHubOption.DisConnected += input_DisConnected;
		sTNodeHubOption2.Connected += output_Connected;
		sTNodeHubOption2.DisConnected += output_DisConnected;
		base.Height = base.TitleHeight + base.InputOptions.Count * 20;
	}

	protected virtual void output_DisConnected(object sender, STNodeOptionEventArgs e)
	{
		STNodeOption sTNodeOption = sender as STNodeOption;
		if (sTNodeOption.ConnectionCount != 0)
		{
			return;
		}
		int index = base.OutputOptions.IndexOf(sTNodeOption);
		if (base.InputOptions[index].ConnectionCount == 0)
		{
			base.InputOptions.RemoveAt(index);
			base.OutputOptions.RemoveAt(index);
			if (base.Owner != null)
			{
				base.Owner.BuildLinePath();
			}
			base.Height -= 20;
		}
	}

	protected virtual void output_Connected(object sender, STNodeOptionEventArgs e)
	{
		STNodeOption sTNodeOption = sender as STNodeOption;
		int index = base.OutputOptions.IndexOf(sTNodeOption);
		Type typeFromHandle = typeof(object);
		if (!(base.InputOptions[index].DataType == typeFromHandle))
		{
			return;
		}
		sTNodeOption.DataType = e.TargetOption.DataType;
		base.InputOptions[index].DataType = sTNodeOption.DataType;
		foreach (STNodeOption inputOption in base.InputOptions)
		{
			if (inputOption.DataType == typeFromHandle)
			{
				return;
			}
		}
		Addhub();
	}

	protected virtual void input_DisConnected(object sender, STNodeOptionEventArgs e)
	{
		STNodeOption sTNodeOption = sender as STNodeOption;
		if (sTNodeOption.ConnectionCount != 0)
		{
			return;
		}
		int index = base.InputOptions.IndexOf(sTNodeOption);
		if (base.OutputOptions[index].ConnectionCount == 0)
		{
			base.InputOptions.RemoveAt(index);
			base.OutputOptions.RemoveAt(index);
			if (base.Owner != null)
			{
				base.Owner.BuildLinePath();
			}
			base.Height -= 20;
		}
	}

	protected virtual void input_DataTransfer(object sender, STNodeOptionEventArgs e)
	{
		STNodeOption option = sender as STNodeOption;
		int index = base.InputOptions.IndexOf(option);
		if (e.Status != ConnectionStatus.Connected)
		{
			base.OutputOptions[index].Data = null;
		}
		else
		{
			base.OutputOptions[index].Data = e.TargetOption.Data;
		}
		base.OutputOptions[index].TransferData();
	}

	protected virtual void input_Connected(object sender, STNodeOptionEventArgs e)
	{
		STNodeOption sTNodeOption = sender as STNodeOption;
		int index = base.InputOptions.IndexOf(sTNodeOption);
		Type typeFromHandle = typeof(object);
		if (sTNodeOption.DataType == typeFromHandle)
		{
			sTNodeOption.DataType = e.TargetOption.DataType;
			base.OutputOptions[index].DataType = sTNodeOption.DataType;
			foreach (STNodeOption inputOption in base.InputOptions)
			{
				if (inputOption.DataType == typeFromHandle)
				{
					return;
				}
			}
			Addhub();
		}
		else
		{
			base.OutputOptions[index].TransferData(e.TargetOption.Data);
		}
	}

	protected override void OnSaveNode(Dictionary<string, byte[]> dic)
	{
		dic.Add("count", BitConverter.GetBytes(base.InputOptionsCount));
	}

	protected internal override void OnLoadNode(Dictionary<string, byte[]> dic)
	{
		base.OnLoadNode(dic);
		int num = BitConverter.ToInt32(dic["count"], 0);
		while (base.InputOptionsCount < num && base.InputOptionsCount != num)
		{
			Addhub();
		}
	}

	public class STNodeHubOption : global::ST.Library.UI.NodeEditor.STNodeHubOption
	{
		public STNodeHubOption(string strText, Type dataType, bool bSingle)
			: base(strText, dataType, bSingle)
		{
		}
	}
}
