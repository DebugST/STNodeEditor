using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using ST.Library.UI;

namespace ST.Library.UI.NodeEditor;

public class STNodeAttribute : Attribute
{
	private string _Path;

	private string _Author;

	private string _Mail;

	private string _Link;

	private string _Description;

	private static char[] m_ch_splitter = new char[2] { '/', '\\' };

	private static Regex m_reg = new Regex("^https?://", RegexOptions.IgnoreCase);

	private static Dictionary<Type, MethodInfo> m_dic = new Dictionary<Type, MethodInfo>();

	public string Path => _Path;

	public string Author => _Author;

	public string Mail => _Mail;

	public string Link => _Link;

	public string Description => _Description;

	public string DisplayDescription => Lang.GetOrDefault(_Description);

	public STNodeAttribute(string strPath)
		: this(strPath, null, null, null, null)
	{
	}

	public STNodeAttribute(string strPath, string strDescription)
		: this(strPath, null, null, null, strDescription)
	{
	}

	public STNodeAttribute(string strPath, string strAuthor, string strMail, string strLink, string strDescription)
	{
		if (!string.IsNullOrEmpty(strPath))
		{
			strPath = strPath.Trim().Trim(m_ch_splitter).Trim();
		}
		_Path = strPath;
		_Author = strAuthor;
		_Mail = strMail;
		_Description = strDescription;
		if (!string.IsNullOrEmpty(strLink) && !(strLink.Trim() == string.Empty))
		{
			strLink = strLink.Trim();
			if (m_reg.IsMatch(strLink))
			{
				_Link = strLink;
			}
			else
			{
				_Link = "http://" + strLink;
			}
		}
	}

	public static MethodInfo GetHelpMethod(Type stNodeType)
	{
		if (m_dic.ContainsKey(stNodeType))
		{
			return m_dic[stNodeType];
		}
		MethodInfo method = stNodeType.GetMethod("ShowHelpInfo");
		if (method == null)
		{
			return null;
		}
		if (!method.IsStatic)
		{
			return null;
		}
		ParameterInfo[] parameters = method.GetParameters();
		if (parameters.Length != 1)
		{
			return null;
		}
		if (parameters[0].ParameterType != typeof(string))
		{
			return null;
		}
		m_dic.Add(stNodeType, method);
		return method;
	}

	public static void ShowHelp(Type stNodeType)
	{
		MethodInfo helpMethod = GetHelpMethod(stNodeType);
		if (!(helpMethod == null))
		{
			helpMethod.Invoke(null, new object[1] { stNodeType.Module.FullyQualifiedName });
		}
	}
}
