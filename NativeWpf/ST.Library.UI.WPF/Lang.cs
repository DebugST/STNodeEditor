using System;
using System.Collections.Generic;
using System.Resources;

namespace ST.Library.UI;

public static class Lang
{
	private static readonly List<ResourceManager> _externalManagers = new();

	public static void RegisterResourceManager(ResourceManager manager)
	{
		if (manager != null && !_externalManagers.Contains(manager))
		{
			_externalManagers.Add(manager);
		}
	}

	public static string Get(string key)
	{
		return GetOrDefault(key);
	}

	public static string GetOrDefault(string key)
	{
		for (int i = _externalManagers.Count - 1; i >= 0; i--)
		{
			try
			{
				string value = _externalManagers[i].GetString(key);
				if (value != null) return value;
			}
			catch { }
		}

		return key;
	}

}
