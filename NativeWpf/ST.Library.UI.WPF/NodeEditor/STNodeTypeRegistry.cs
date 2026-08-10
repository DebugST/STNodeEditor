using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ST.Library.UI.NodeEditor;

internal static class STNodeTypeRegistry
{
	private const int InitializationNotStarted = 0;
	private const int InitializationInProgress = 1;
	private const int InitializationCompleted = 2;
	private static readonly Type NodeType = typeof(STNode);
	private static readonly string NodeAssemblyName = NodeType.Assembly.GetName().Name;
	private static readonly object InitializationSyncRoot = new object();
	private static readonly object SyncRoot = new object();
	private static readonly ConcurrentQueue<Assembly> PendingAssemblies = new ConcurrentQueue<Assembly>();
	private static readonly HashSet<Type> NodeTypes = new HashSet<Type>();
	private static readonly Dictionary<string, Type> GuidTypes = new Dictionary<string, Type>();
	private static readonly Dictionary<string, Type> ModelTypes = new Dictionary<string, Type>();
	private static readonly HashSet<string> AmbiguousModels = new HashSet<string>();
	private static readonly Dictionary<Assembly, List<Type>> AssemblyTypes = new Dictionary<Assembly, List<Type>>();
	private static int _initializationState;
	private static bool _assemblyLoadSubscribed;
	[ThreadStatic]
	private static bool _isScanningLoadedAssemblies;

	public static void Initialize()
	{
		if (System.Threading.Volatile.Read(ref _initializationState) == InitializationCompleted)
		{
			return;
		}

		lock (InitializationSyncRoot)
		{
			if (!_assemblyLoadSubscribed)
			{
				AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
				_assemblyLoadSubscribed = true;
			}
		}

		int previousState = System.Threading.Interlocked.CompareExchange(
			ref _initializationState,
			InitializationInProgress,
			InitializationNotStarted);
		if (previousState == InitializationCompleted)
		{
			return;
		}
		if (previousState == InitializationInProgress)
		{
			ScanLoadedAssemblies();
			return;
		}

		try
		{
			ScanLoadedAssemblies();
			System.Threading.Volatile.Write(ref _initializationState, InitializationCompleted);
		}
		catch
		{
			System.Threading.Volatile.Write(ref _initializationState, InitializationNotStarted);
			throw;
		}
	}

	private static void ScanLoadedAssemblies()
	{
		// Reflection may load more assemblies and re-enter Initialize on the same thread.
		if (_isScanningLoadedAssemblies)
		{
			return;
		}

		_isScanningLoadedAssemblies = true;
		try
		{
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				RegisterAssemblyCore(assembly);
			}
			RegisterPendingAssemblies();
		}
		finally
		{
			_isScanningLoadedAssemblies = false;
		}
	}

	public static int LoadAssemblies(IEnumerable<Assembly> assemblies)
	{
		Initialize();
		int count = 0;
		foreach (Assembly assembly in assemblies)
		{
			if (RegisterAssemblyCore(assembly))
			{
				count++;
			}
		}
		RegisterPendingAssemblies();
		return count;
	}

	public static bool LoadAssembly(string strFile)
	{
		Assembly assembly = Assembly.LoadFrom(strFile);
		return LoadAssembly(assembly);
	}

	public static bool LoadAssembly(Assembly assembly)
	{
		if (assembly == null)
		{
			return false;
		}
		Initialize();
		bool containsNodeTypes = RegisterAssemblyCore(assembly);
		RegisterPendingAssemblies();
		return containsNodeTypes;
	}

	public static Type[] GetTypes()
	{
		Initialize();
		RegisterPendingAssemblies();
		lock (SyncRoot)
		{
			return NodeTypes.ToArray();
		}
	}

	public static Type[] GetTypes(Assembly assembly)
	{
		Initialize();
		RegisterPendingAssemblies();
		lock (SyncRoot)
		{
			if (assembly != null && AssemblyTypes.TryGetValue(assembly, out List<Type> types))
			{
				return types.ToArray();
			}
			return Array.Empty<Type>();
		}
	}

	public static Assembly[] GetAssemblies()
	{
		Initialize();
		RegisterPendingAssemblies();
		lock (SyncRoot)
		{
			return AssemblyTypes
				.Where(pair => pair.Value.Count > 0)
				.Select(pair => pair.Key)
				.ToArray();
		}
	}

	public static bool TryGetNodeType(string guid, string model, out Type type)
	{
		Initialize();
		RegisterPendingAssemblies();
		lock (SyncRoot)
		{
			if (!string.IsNullOrEmpty(guid) && GuidTypes.TryGetValue(guid, out type))
			{
				return true;
			}
			if (!string.IsNullOrEmpty(model) && ModelTypes.TryGetValue(model, out type))
			{
				return true;
			}
			if (TryGetNodeTypeByLegacySuffix(model, out type))
			{
				return true;
			}
			type = null;
			return false;
		}
	}

	public static string GetModelByType(Type type)
	{
		return $"{type.Module.Name}|{type.FullName}";
	}

	private static bool TryGetNodeTypeByLegacySuffix(string model, out Type type)
	{
		type = null;
		if (string.IsNullOrEmpty(model))
		{
			return false;
		}

		int moduleSeparator = model.IndexOf('|');
		if (moduleSeparator <= 0 || moduleSeparator >= model.Length - 1)
		{
			return false;
		}
		string legacyTypeName = model.Substring(moduleSeparator + 1);
		int typeNameSeparator = Math.Max(legacyTypeName.LastIndexOf('.'), legacyTypeName.LastIndexOf('+'));
		if (typeNameSeparator < 0 || typeNameSeparator >= legacyTypeName.Length - 1)
		{
			return false;
		}

		string currentModel = string.Concat(model.AsSpan(0, moduleSeparator + 1), legacyTypeName.AsSpan(typeNameSeparator + 1));
		return ModelTypes.TryGetValue(currentModel, out type);
	}

	private static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
	{
		// AssemblyLoad runs inside runtime loader coordination; never reflect or wait here.
		PendingAssemblies.Enqueue(args.LoadedAssembly);
	}

	private static void RegisterPendingAssemblies()
	{
		while (PendingAssemblies.TryDequeue(out Assembly assembly))
		{
			RegisterAssemblyCore(assembly);
		}
	}

	private static bool RegisterAssemblyCore(Assembly assembly)
	{
		if (!ShouldScanAssembly(assembly))
		{
			return false;
		}

		lock (SyncRoot)
		{
			if (AssemblyTypes.TryGetValue(assembly, out List<Type> existingTypes))
			{
				return existingTypes.Count > 0;
			}
		}

		NodeTypeRegistration[] registrations = GetLoadableTypes(assembly)
			.Where(IsNodeType)
			.Select(type => new NodeTypeRegistration(
				type,
				type.GUID.ToString(),
				GetModelByType(type),
				$"{type.Module.Name}|{type.Name}"))
			.ToArray();

		lock (SyncRoot)
		{
			if (AssemblyTypes.TryGetValue(assembly, out List<Type> existingTypes))
			{
				return existingTypes.Count > 0;
			}

			List<Type> registeredTypes = new List<Type>();
			AssemblyTypes.Add(assembly, registeredTypes);
			foreach (NodeTypeRegistration registration in registrations)
			{
				if (!NodeTypes.Add(registration.Type))
				{
					continue;
				}

				registeredTypes.Add(registration.Type);
				if (!GuidTypes.ContainsKey(registration.Guid))
				{
					GuidTypes.Add(registration.Guid, registration.Type);
				}

				RegisterModelKey(registration.Model, registration.Type);
				RegisterModelKey(registration.ShortModel, registration.Type);
			}
			return registeredTypes.Count > 0;
		}
	}

	private static void RegisterModelKey(string model, Type type)
	{
		if (AmbiguousModels.Contains(model))
		{
			return;
		}
		if (ModelTypes.TryGetValue(model, out Type existingType) && existingType != type)
		{
			ModelTypes.Remove(model);
			AmbiguousModels.Add(model);
			return;
		}
		ModelTypes[model] = type;
	}

	private static bool ShouldScanAssembly(Assembly assembly)
	{
		if (assembly == null || assembly.IsDynamic)
		{
			return false;
		}
		if (assembly == NodeType.Assembly)
		{
			return true;
		}
		try
		{
			foreach (AssemblyName referencedAssembly in assembly.GetReferencedAssemblies())
			{
				if (string.Equals(referencedAssembly.Name, NodeAssemblyName, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static Type[] GetLoadableTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			return ex.Types.Where(type => type != null).ToArray();
		}
		catch
		{
			return Array.Empty<Type>();
		}
	}

	private static bool IsNodeType(Type type)
	{
		return type != null
			&& type.IsClass
			&& !type.IsAbstract
			&& NodeType.IsAssignableFrom(type);
	}

	private sealed class NodeTypeRegistration
	{
		public Type Type { get; }
		public string Guid { get; }
		public string Model { get; }
		public string ShortModel { get; }

		public NodeTypeRegistration(Type type, string guid, string model, string shortModel)
		{
			Type = type;
			Guid = guid;
			Model = model;
			ShortModel = shortModel;
		}
	}
}
