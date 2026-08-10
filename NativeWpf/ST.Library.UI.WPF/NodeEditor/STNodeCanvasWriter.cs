using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace ST.Library.UI.NodeEditor;

/// <summary>
/// Writes the version-1 STND canvas envelope shared by visual and headless
/// graph hosts.
/// </summary>
public static class STNodeCanvasWriter
{
	public static void Write(
		Stream stream,
		IEnumerable<STNode> nodes,
		IEnumerable<ConnectionInfo> connections,
		float canvasOffsetX,
		float canvasOffsetY,
		float canvasScale)
	{
		ArgumentNullException.ThrowIfNull(stream);
		ArgumentNullException.ThrowIfNull(nodes);
		ArgumentNullException.ThrowIfNull(connections);
		if (!stream.CanWrite)
			throw new ArgumentException("The canvas stream must be writable.", nameof(stream));
		if (!float.IsFinite(canvasOffsetX)
			|| !float.IsFinite(canvasOffsetY)
			|| !float.IsFinite(canvasScale)
			|| canvasScale <= 0)
		{
			throw new ArgumentOutOfRangeException(
				nameof(canvasScale),
				"Canvas coordinates and scale must be finite, and scale must be positive.");
		}

		List<STNode> nodeList = nodes.ToList();
		if (nodeList.Any(node => node == null))
			throw new ArgumentException("Canvas nodes cannot contain null.", nameof(nodes));
		if (nodeList.Distinct().Count() != nodeList.Count)
			throw new ArgumentException("Canvas nodes cannot contain duplicates.", nameof(nodes));

		var nodeIndices = nodeList
			.Select((node, index) => (node, index))
			.ToDictionary(item => item.node, item => item.index);
		var optionIndices = new Dictionary<STNodeOption, long>();
		var nodeData = new List<byte[]>(nodeList.Count);
		foreach (STNode node in nodeList)
		{
			try
			{
				nodeData.Add(node.GetSaveData());
				AddOptions(optionIndices, node.GetAllInputOptions());
				AddOptions(optionIndices, node.GetAllOutputOptions());
			}
			catch (Exception ex)
			{
				throw new InvalidDataException(
					$"Failed to serialize node '{node.Title}'.",
					ex);
			}
		}

		var uniqueConnections = new HashSet<(STNodeOption Output, STNodeOption Input)>();
		ConnectionInfo[] orderedConnections = connections
			.Where(connection =>
				connection.Output != null
				&& connection.Input != null
				&& uniqueConnections.Add((connection.Output, connection.Input)))
			.OrderBy(connection => GetNodeIndex(nodeIndices, connection.Output.Owner))
			.ThenBy(connection => GetOptionIndex(connection.Output.Owner.GetAllOutputOptions(), connection.Output))
			.ThenBy(connection => GetNodeIndex(nodeIndices, connection.Input.Owner))
			.ThenBy(connection => GetOptionIndex(connection.Input.Owner.GetAllInputOptions(), connection.Input))
			.ToArray();
		var packedConnections = new long[orderedConnections.Length];
		for (int i = 0; i < orderedConnections.Length; i++)
		{
			ConnectionInfo connection = orderedConnections[i];
			ValidateConnection(nodeIndices, optionIndices, connection);
			packedConnections[i] =
				optionIndices[connection.Output] << 32
				| unchecked((uint)optionIndices[connection.Input]);
		}

		WriteRaw(
			stream,
			nodeData,
			packedConnections,
			canvasOffsetX,
			canvasOffsetY,
			canvasScale);
	}

	public static ConnectionInfo[] GetConnections(IEnumerable<STNode> nodes)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		List<STNode> nodeList = nodes.ToList();
		var nodeSet = new HashSet<STNode>(nodeList);
		var connections = new List<ConnectionInfo>();
		var uniqueConnections = new HashSet<(STNodeOption Output, STNodeOption Input)>();
		foreach (STNode node in nodeList)
		{
			if (node == null)
				throw new ArgumentException("Canvas nodes cannot contain null.", nameof(nodes));
			foreach (STNodeOption output in node.GetAllOutputOptions())
			{
				if (output == null || ReferenceEquals(output, STNodeOption.Empty))
					continue;
				foreach (STNodeOption input in output.ConnectedOption)
				{
					if (input == null
						|| !input.IsInput
						|| input.Owner == null
						|| !nodeSet.Contains(input.Owner)
						|| !uniqueConnections.Add((output, input)))
					{
						continue;
					}
					connections.Add(new ConnectionInfo
					{
						Output = output,
						Input = input
					});
				}
			}
		}
		return connections.ToArray();
	}

	/// <summary>
	/// Writes already serialized node payloads and packed global option
	/// indices using the unchanged STND v1 envelope.
	/// </summary>
	public static void WriteRaw(
		Stream stream,
		IReadOnlyList<byte[]> nodeData,
		IReadOnlyList<long> packedConnections,
		float canvasOffsetX,
		float canvasOffsetY,
		float canvasScale)
	{
		ArgumentNullException.ThrowIfNull(stream);
		ArgumentNullException.ThrowIfNull(nodeData);
		ArgumentNullException.ThrowIfNull(packedConnections);
		if (!stream.CanWrite)
			throw new ArgumentException("The canvas stream must be writable.", nameof(stream));

		stream.Write(STNodeConstant.NodeFlag, 0, STNodeConstant.NodeFlag.Length);
		stream.WriteByte(STNodeConstant.Version);
		using GZipStream gzip = new GZipStream(stream, CompressionMode.Compress);
		WriteSingle(gzip, canvasOffsetX);
		WriteSingle(gzip, canvasOffsetY);
		WriteSingle(gzip, canvasScale);
		WriteInt32(gzip, nodeData.Count);
		foreach (byte[] payload in nodeData)
		{
			if (payload == null || payload.Length == 0)
				throw new InvalidDataException("A serialized node payload is empty.");
			WriteInt32(gzip, payload.Length);
			gzip.Write(payload, 0, payload.Length);
		}
		WriteInt32(gzip, packedConnections.Count);
		foreach (long connection in packedConnections)
		{
			byte[] bytes = BitConverter.GetBytes(connection);
			gzip.Write(bytes, 0, bytes.Length);
		}
	}

	private static void AddOptions(
		Dictionary<STNodeOption, long> optionIndices,
		IEnumerable<STNodeOption> options)
	{
		foreach (STNodeOption option in options)
		{
			if (option != null && !optionIndices.ContainsKey(option))
				optionIndices.Add(option, optionIndices.Count);
		}
	}

	private static int GetNodeIndex(
		Dictionary<STNode, int> nodeIndices,
		STNode node)
	{
		if (node == null || !nodeIndices.TryGetValue(node, out int index))
			throw new InvalidDataException("A canvas connection references a node outside the canvas.");
		return index;
	}

	private static int GetOptionIndex(
		STNodeOption[] options,
		STNodeOption option)
	{
		for (int i = 0; i < options.Length; i++)
		{
			if (ReferenceEquals(options[i], option))
				return i;
		}
		throw new InvalidDataException("A canvas connection references an unknown node option.");
	}

	private static void ValidateConnection(
		Dictionary<STNode, int> nodeIndices,
		Dictionary<STNodeOption, long> optionIndices,
		ConnectionInfo connection)
	{
		if (connection.Output == null
			|| connection.Input == null
			|| connection.Output.IsInput
			|| !connection.Input.IsInput
			|| ReferenceEquals(connection.Output, STNodeOption.Empty)
			|| ReferenceEquals(connection.Input, STNodeOption.Empty)
			|| connection.Output.Owner == null
			|| connection.Input.Owner == null
			|| ReferenceEquals(connection.Output.Owner, connection.Input.Owner)
			|| !nodeIndices.ContainsKey(connection.Output.Owner)
			|| !nodeIndices.ContainsKey(connection.Input.Owner)
			|| !optionIndices.ContainsKey(connection.Output)
			|| !optionIndices.ContainsKey(connection.Input))
		{
			throw new InvalidDataException("The canvas contains an invalid connection.");
		}
	}

	private static void WriteSingle(GZipStream stream, float value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		stream.Write(bytes, 0, bytes.Length);
	}

	private static void WriteInt32(GZipStream stream, int value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		stream.Write(bytes, 0, bytes.Length);
	}
}
