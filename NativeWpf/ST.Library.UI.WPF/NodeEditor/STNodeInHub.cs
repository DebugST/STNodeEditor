using System;
using System.Collections.Generic;
using System.Drawing;

namespace ST.Library.UI.NodeEditor;

public class STNodeInHub : STNode
{
	private bool m_bSingle;

	private string m_strIn;

	public STNodeInHub()
		: this(bSingle: false)
	{
	}

	public STNodeInHub(string title)
		: this(bSingle: false, title)
	{
	}

	public STNodeInHub(bool bSingle)
		: this(bSingle, "InHUB")
	{
	}

	public STNodeInHub(bool bSingle, string title)
		: this(bSingle, "NodeIN", title)
	{
	}

	public STNodeInHub(bool bSingle, string strTextIn, string title)
	{
		m_bSingle = bSingle;
		m_strIn = strTextIn;
		Addhub();
		base.Title = Lang.Get(title);
		base.AutoSize = true;
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
		base.InputOptions.Add(sTNodeHubOption);
		sTNodeHubOption.Connected += input_Connected;
		sTNodeHubOption.DataTransfer += input_DataTransfer;
		sTNodeHubOption.DisConnected += input_DisConnected;
		base.Height = base.TitleHeight + base.InputOptions.Count * 20;
	}

	protected virtual void DoInputDisConnected(STNodeOption sender, STNodeOptionEventArgs e)
	{
	}

	private void input_DisConnected(object sender, STNodeOptionEventArgs e)
	{
		STNodeOption sTNodeOption = sender as STNodeOption;
		if (sTNodeOption.ConnectionCount != 0)
		{
			DoInputDisConnected(sTNodeOption, e);
			return;
		}
		int index = base.InputOptions.IndexOf(sTNodeOption);
		base.InputOptions.RemoveAt(index);
		if (base.Owner != null)
		{
			base.Owner.BuildLinePath();
		}
		base.Height -= 20;
		DoInputDisConnected(sTNodeOption, e);
	}

	protected virtual void DoInputDataTransfer(STNodeOption sender, STNodeOptionEventArgs e)
	{
	}

	private void input_DataTransfer(object sender, STNodeOptionEventArgs e)
	{
		DoInputDataTransfer(sender as STNodeOption, e);
	}

	protected virtual void DoInputConnected(STNodeOption sender, STNodeOptionEventArgs e)
	{
	}

	private void input_Connected(object sender, STNodeOptionEventArgs e)
	{
		STNodeOption sTNodeOption = sender as STNodeOption;
		Type typeFromHandle = typeof(object);
		if (sTNodeOption.DataType == typeFromHandle)
		{
			sTNodeOption.DataType = e.TargetOption.DataType;
			foreach (STNodeOption inputOption in base.InputOptions)
			{
				if (inputOption.DataType == typeFromHandle)
				{
					DoInputConnected(sTNodeOption, e);
					return;
				}
			}
			Addhub();
		}
		DoInputConnected(sTNodeOption, e);
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
}
