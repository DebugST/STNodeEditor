using System;
using System.Collections.Generic;
using System.Drawing;

namespace ST.Library.UI.NodeEditor;

public class STNodeOutHub : STNode
{
	private bool m_bSingle;

	private string m_strOut;

	public STNodeOutHub()
		: this(bSingle: false)
	{
	}

	public STNodeOutHub(bool bSingle)
		: this(bSingle, "HUB")
	{
	}

	public STNodeOutHub(bool bSingle, string title)
		: this(bSingle, "OUT", title)
	{
	}

	public STNodeOutHub(string title)
		: this(bSingle: false, title)
	{
	}

	public STNodeOutHub(bool bSingle, string strTextOut, string title)
	{
		m_bSingle = bSingle;
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

	protected virtual void Addhub()
	{
		STNodeHubOption sTNodeHubOption = new STNodeHubOption(m_strOut, typeof(object), m_bSingle);
		base.OutputOptions.Add(sTNodeHubOption);
		sTNodeHubOption.Connected += output_Connected;
		sTNodeHubOption.DisConnected += output_DisConnected;
		base.Height = base.TitleHeight + base.OutputOptions.Count * 20;
	}

	protected virtual void DoOutputDisConnected(STNodeOption sender, STNodeOptionEventArgs e)
	{
	}

	private void output_DisConnected(object sender, STNodeOptionEventArgs e)
	{
		STNodeOption sTNodeOption = sender as STNodeOption;
		if (sTNodeOption.ConnectionCount != 0)
		{
			DoOutputDisConnected(sTNodeOption, e);
			return;
		}
		int index = base.OutputOptions.IndexOf(sTNodeOption);
		if (base.OutputOptions[index].ConnectionCount == 0)
		{
			base.OutputOptions.RemoveAt(index);
			if (base.Owner != null)
			{
				base.Owner.BuildLinePath();
			}
			base.Height -= 20;
			DoOutputDisConnected(sTNodeOption, e);
		}
	}

	protected virtual void DoOutputConnected(STNodeOption sender, STNodeOptionEventArgs e)
	{
	}

	private void output_Connected(object sender, STNodeOptionEventArgs e)
	{
		STNodeOption sender2 = sender as STNodeOption;
		Type typeFromHandle = typeof(object);
		foreach (STNodeOption outputOption in base.OutputOptions)
		{
			if (outputOption.DataType == typeFromHandle)
			{
				DoOutputConnected(sender2, e);
				return;
			}
		}
		Addhub();
		DoOutputConnected(sender2, e);
	}

	protected override void OnSaveNode(Dictionary<string, byte[]> dic)
	{
		dic.Add("count", BitConverter.GetBytes(base.OutputOptionsCount));
	}

	protected internal override void OnLoadNode(Dictionary<string, byte[]> dic)
	{
		base.OnLoadNode(dic);
		int num = BitConverter.ToInt32(dic["count"], 0);
		while (base.OutputOptionsCount < num && base.OutputOptionsCount != num)
		{
			Addhub();
		}
	}
}
