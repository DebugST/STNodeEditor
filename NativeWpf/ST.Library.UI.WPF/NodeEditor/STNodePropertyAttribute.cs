using System;

namespace ST.Library.UI.NodeEditor;

public class STNodePropertyAttribute : Attribute
{
	private string _Name;

	private string _Description;

	private Type _ConverterType = typeof(STNodePropertyDescriptor);

	private bool _IsEditEnable;

	private bool _IsReadOnly;

	private bool _IsHide;

	public string Name => _Name;

	public string Description => _Description;

	public Type DescriptorType
	{
		get
		{
			return _ConverterType;
		}
		set
		{
			_ConverterType = value;
		}
	}

	public bool IsEditEnable => _IsEditEnable;

	public bool IsReadOnly => _IsReadOnly;

	public bool IsHide
	{
		get
		{
			return _IsHide;
		}
		set
		{
			_IsHide = value;
		}
	}

	public STNodePropertyAttribute(string strKey, string strDesc)
		: this(strKey, strDesc, isEditEnable: false)
	{
	}

	public STNodePropertyAttribute(string strKey, string strDesc, bool isEditEnable)
		: this(strKey, strDesc, isEditEnable, isHide: false)
	{
	}

	public STNodePropertyAttribute(string strKey, string strDesc, bool isEditEnable, bool isHide)
		: this(strKey, strDesc, isEditEnable, isHide, isReadOnly: false)
	{
	}

	public STNodePropertyAttribute(string strKey, string strDesc, bool isEditEnable, bool isHide, bool isReadOnly)
	{
		_Name = strKey;
		_Description = strDesc;
		_IsEditEnable = isEditEnable;
		_IsHide = isHide;
		_IsReadOnly = isReadOnly;
	}
}
