#pragma warning disable CA1859
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace ST.Library.UI.NodeEditor;

public partial class STNodeEditor
{
	public const string ClipboardFormatV1 = "STNodeEditor_Nodes_V1";

	private const int MaximumImportedNodeCount = 10000;
	private const int MaximumImportedConnectionCount = 100000;
	private const int MaximumNodeDataLength = 16 * 1024 * 1024;
	private const long MaximumTotalNodeDataLength = 128L * 1024 * 1024;
	private const int MaximumDecompressedGraphLength = 160 * 1024 * 1024;
	private static readonly uint[] Crc32Table = CreateCrc32Table();

	private sealed class GraphConnectionReference
	{
		public STNodeOption Output { get; }

		public STNodeOption Input { get; }

		public GraphConnectionReference(STNodeOption output, STNodeOption input)
		{
			Output = output;
			Input = input;
		}
	}

	private sealed class GraphImportPlan
	{
		public List<STNode> Nodes { get; }

		public List<GraphConnectionReference> Connections { get; }

		public Point SourceOrigin { get; }

		public GraphImportPlan(List<STNode> nodes, List<GraphConnectionReference> connections)
		{
			Nodes = nodes;
			Connections = connections;
			SourceOrigin = nodes.Count == 0
				? Point.Empty
				: new Point(nodes.Min(node => node.Left), nodes.Min(node => node.Top));
		}
	}

	public byte[] GetSelectedNodesData()
	{
		return GetNodesData(GetSelectedNode());
	}

	public byte[] GetNodesData(IEnumerable<STNode> nodes)
	{
		if (nodes == null)
		{
			throw new ArgumentNullException(nameof(nodes));
		}
		HashSet<STNode> requestedNodes = new HashSet<STNode>(nodes);
		List<STNode> orderedNodes = Nodes
			.Cast<STNode>()
			.Where(requestedNodes.Contains)
			.ToList();
		if (orderedNodes.Count == 0)
		{
			return Array.Empty<byte>();
		}

		Dictionary<STNodeOption, long> optionIndexes = BuildOptionIndexes(orderedNodes);
		List<GraphConnectionReference> connections = GetInternalConnections(orderedNodes, requestedNodes, optionIndexes);
		using MemoryStream stream = new MemoryStream();
		using (GZipStream gzip = new GZipStream(stream, CompressionMode.Compress, leaveOpen: true))
		{
			WriteInt32(gzip, orderedNodes.Count);
			WriteInt32(gzip, orderedNodes.Min(node => node.Left));
			WriteInt32(gzip, orderedNodes.Min(node => node.Top));
			foreach (STNode node in orderedNodes)
			{
				byte[] nodeData = node.GetSaveData();
				WriteInt32(gzip, nodeData.Length);
				gzip.Write(nodeData, 0, nodeData.Length);
			}
			WriteInt32(gzip, connections.Count);
			foreach (GraphConnectionReference connection in connections)
			{
				long packed = optionIndexes[connection.Output] << 32
					| optionIndexes[connection.Input] & uint.MaxValue;
				byte[] bytes = BitConverter.GetBytes(packed);
				gzip.Write(bytes, 0, bytes.Length);
			}
		}
		return stream.ToArray();
	}

	public IReadOnlyList<STNode> ImportSelectionData(byte[] data, Point targetCanvasPoint)
	{
		GraphImportPlan plan = DecodeSelectionData(data);
		MovePlanTo(plan, targetCanvasPoint);
		return CommitImportPlan(plan, "粘贴节点");
	}

	public IReadOnlyList<STNode> ImportSelectionData(byte[] data)
	{
		GraphImportPlan plan = DecodeSelectionData(data);
		foreach (STNode node in plan.Nodes)
		{
			MoveDetachedNode(node, node.Left + 30, node.Top + 30);
		}
		return CommitImportPlan(plan, "粘贴节点");
	}

	public IReadOnlyList<STNode> ImportCanvasAsModule(byte[] canvasData, Point targetCanvasPoint)
	{
		GraphImportPlan plan = DecodeCanvasData(canvasData);
		MovePlanTo(plan, targetCanvasPoint);
		return CommitImportPlan(plan, "导入流程模块");
	}

	public bool CopySelectionToClipboard()
	{
		byte[] data = GetSelectedNodesData();
		if (data.Length == 0)
		{
			return false;
		}
		try
		{
			System.Windows.Clipboard.SetData(ClipboardFormatV1, Convert.ToBase64String(data));
			return true;
		}
		catch
		{
			return false;
		}
	}

	public bool CutSelectionToClipboard()
	{
		if (!EnableEdit || !CopySelectionToClipboard())
		{
			return false;
		}
		return DeleteSelectedNodes();
	}

	public IReadOnlyList<STNode> PasteFromClipboard()
	{
		if (!EnableEdit || !ClipboardContainsGraph())
		{
			return Array.Empty<STNode>();
		}
		string base64 = System.Windows.Clipboard.GetData(ClipboardFormatV1) as string;
		if (string.IsNullOrWhiteSpace(base64))
		{
			return Array.Empty<STNode>();
		}
		byte[] data = Convert.FromBase64String(base64);
		if (IsMouseOver)
		{
			System.Windows.Point position = Mouse.GetPosition(this);
			Point target = ControlToCanvas(new Point((int)Math.Round(position.X), (int)Math.Round(position.Y)));
			return ImportSelectionData(data, target);
		}
		return ImportSelectionData(data);
	}

	private void ExecutePasteCommand()
	{
		try
		{
			PasteFromClipboard();
		}
		catch
		{
			// Routed commands must not tear down the WPF input pipeline.
		}
	}

	internal bool ClipboardContainsGraph()
	{
		try
		{
			return System.Windows.Clipboard.ContainsData(ClipboardFormatV1);
		}
		catch
		{
			return false;
		}
	}

	private GraphImportPlan DecodeSelectionData(byte[] data)
	{
		if (data == null || data.Length == 0)
		{
			throw new InvalidDataException("节点数据为空");
		}
		using MemoryStream stream = new MemoryStream(DecompressGZip(data, 0), writable: false);
		int nodeCount = ReadCount(stream, MaximumImportedNodeCount, "节点");
		ReadInt32(stream);
		ReadInt32(stream);
		return DecodeGraphBody(stream, nodeCount);
	}

	private GraphImportPlan DecodeCanvasData(byte[] data)
	{
		if (data == null || data.Length < STNodeConstant.NodeFlag.Length + 1)
		{
			throw new InvalidDataException("流程模块数据为空或不完整");
		}
		byte[] header = new byte[STNodeConstant.NodeFlag.Length + 1];
		Array.Copy(data, header, header.Length);
		for (int i = 0; i < STNodeConstant.NodeFlag.Length; i++)
		{
			if (header[i] != STNodeConstant.NodeFlag[i])
			{
				throw new InvalidDataException("无法识别的流程模块格式");
			}
		}
		if (header[STNodeConstant.NodeFlag.Length] != STNodeConstant.Version)
		{
			throw new InvalidDataException("无法识别的流程模块版本");
		}
		using MemoryStream stream = new MemoryStream(DecompressGZip(data, header.Length), writable: false);
		ReadBytes(stream, 12);
		int nodeCount = ReadCount(stream, MaximumImportedNodeCount, "节点");
		return DecodeGraphBody(stream, nodeCount);
	}

	private GraphImportPlan DecodeGraphBody(Stream stream, int nodeCount)
	{
		List<STNode> nodes = new List<STNode>(nodeCount);
		Dictionary<long, STNodeOption> options = new Dictionary<long, STNodeOption>();
		long totalNodeDataLength = 0;
		for (int i = 0; i < nodeCount; i++)
		{
			int nodeDataLength = ReadInt32(stream);
			if (nodeDataLength <= 0 || nodeDataLength > MaximumNodeDataLength)
			{
				throw new InvalidDataException($"节点数据长度无效：{nodeDataLength}");
			}
			totalNodeDataLength += nodeDataLength;
			if (totalNodeDataLength > MaximumTotalNodeDataLength)
			{
				throw new InvalidDataException("节点数据总长度超过限制");
			}
			byte[] nodeData = ReadBytes(stream, nodeDataLength);
			STNode node;
			try
			{
				node = GetNodeFromData(nodeData);
				node.RegenerateGuid();
			}
			catch (Exception ex)
			{
				throw new InvalidDataException($"第 {i + 1} 个节点无法加载", ex);
			}
			nodes.Add(node);
			AddOptions(options, node);
		}

		int connectionCount = ReadCount(stream, MaximumImportedConnectionCount, "连接");
		List<GraphConnectionReference> connections = new List<GraphConnectionReference>(connectionCount);
		HashSet<string> connectionKeys = new HashSet<string>(StringComparer.Ordinal);
		for (int i = 0; i < connectionCount; i++)
		{
			long packed = BitConverter.ToInt64(ReadBytes(stream, 8), 0);
			long outputIndex = packed >> 32;
			long inputIndex = unchecked((uint)packed);
			if (!options.TryGetValue(outputIndex, out STNodeOption output)
				|| !options.TryGetValue(inputIndex, out STNodeOption input))
			{
				throw new InvalidDataException($"第 {i + 1} 条连接引用了不存在的端口");
			}
			if (output.IsInput || !input.IsInput || output.Owner == input.Owner)
			{
				throw new InvalidDataException($"第 {i + 1} 条连接方向无效");
			}
			string key = outputIndex + ":" + inputIndex;
			if (!connectionKeys.Add(key))
			{
				throw new InvalidDataException($"第 {i + 1} 条连接重复");
			}
			connections.Add(new GraphConnectionReference(output, input));
		}
		if (stream.ReadByte() != -1)
		{
			throw new InvalidDataException("节点数据包含未识别的尾部内容");
		}
		return new GraphImportPlan(nodes, connections);
	}

	private IReadOnlyList<STNode> CommitImportPlan(GraphImportPlan plan, string description)
	{
		if (plan.Nodes.Count == 0)
		{
			return Array.Empty<STNode>();
		}
		STNode[] selectedBefore = GetSelectedNode();
		STNode activeBefore = ActiveNode;
		List<STNode> addedNodes = new List<STNode>(plan.Nodes.Count);
		using STNodeEditTransaction transaction = BeginEditTransaction(description);
		try
		{
			foreach (STNode node in plan.Nodes)
			{
				try
				{
					Nodes.Add(node);
				}
				finally
				{
					if (Nodes.Contains(node))
					{
						addedNodes.Add(node);
					}
				}
			}
			foreach (GraphConnectionReference connection in plan.Connections)
			{
				bool outputLocked = connection.Output.Owner.LockOption;
				bool inputLocked = connection.Input.Owner.LockOption;
				connection.Output.Owner.LockOption = false;
				connection.Input.Owner.LockOption = false;
				try
				{
					ConnectionStatus status = connection.Output.ConnectOption(connection.Input);
					if (status != ConnectionStatus.Connected)
					{
						throw new InvalidOperationException($"无法恢复导入节点的连接：{status}");
					}
				}
				finally
				{
					connection.Output.Owner.LockOption = outputLocked;
					connection.Input.Owner.LockOption = inputLocked;
				}
			}
			foreach (STNode node in addedNodes)
			{
				node.OnEditorLoadCompleted();
			}
			foreach (STNode node in selectedBefore)
			{
				RemoveSelectedNode(node);
			}
			foreach (STNode node in addedNodes)
			{
				AddSelectedNode(node);
			}
			SetActiveNode(addedNodes[addedNodes.Count - 1]);
			Invalidate();
			return addedNodes.AsReadOnly();
		}
		catch
		{
			transaction.Cancel();
			ReplayHistory(() =>
			{
				for (int i = addedNodes.Count - 1; i >= 0; i--)
				{
					Nodes.Remove(addedNodes[i]);
				}
			});
			foreach (STNode node in selectedBefore)
			{
				if (Nodes.Contains(node))
				{
					AddSelectedNode(node);
				}
			}
			SetActiveNode(activeBefore != null && Nodes.Contains(activeBefore) ? activeBefore : null);
			throw;
		}
	}

	private static void MovePlanTo(GraphImportPlan plan, Point target)
	{
		int offsetX = target.X - plan.SourceOrigin.X;
		int offsetY = target.Y - plan.SourceOrigin.Y;
		foreach (STNode node in plan.Nodes)
		{
			MoveDetachedNode(node, node.Left + offsetX, node.Top + offsetY);
		}
	}

	private static void MoveDetachedNode(STNode node, int left, int top)
	{
		bool locked = node.LockLocation;
		node.LockLocation = false;
		node.Location = new Point(left, top);
		node.LockLocation = locked;
	}

	private static Dictionary<STNodeOption, long> BuildOptionIndexes(IEnumerable<STNode> nodes)
	{
		Dictionary<STNodeOption, long> indexes = new Dictionary<STNodeOption, long>();
		foreach (STNode node in nodes)
		{
			foreach (STNodeOption option in node.GetAllInputOptions())
			{
				if (option != null && !indexes.ContainsKey(option))
				{
					indexes.Add(option, indexes.Count);
				}
			}
			foreach (STNodeOption option in node.GetAllOutputOptions())
			{
				if (option != null && !indexes.ContainsKey(option))
				{
					indexes.Add(option, indexes.Count);
				}
			}
		}
		return indexes;
	}

	private static List<GraphConnectionReference> GetInternalConnections(
		IEnumerable<STNode> nodes,
		HashSet<STNode> nodeSet,
		Dictionary<STNodeOption, long> optionIndexes)
	{
		List<GraphConnectionReference> connections = new List<GraphConnectionReference>();
		foreach (STNode node in nodes)
		{
			foreach (STNodeOption output in node.GetAllOutputOptions())
			{
				if (!optionIndexes.ContainsKey(output))
				{
					continue;
				}
				IEnumerable<STNodeOption> inputs = output.ConnectedOption
					.Where(input => input != null && input.IsInput && nodeSet.Contains(input.Owner) && optionIndexes.ContainsKey(input))
					.OrderBy(input => optionIndexes[input]);
				foreach (STNodeOption input in inputs)
				{
					connections.Add(new GraphConnectionReference(output, input));
				}
			}
		}
		return connections;
	}

	private static void AddOptions(Dictionary<long, STNodeOption> options, STNode node)
	{
		foreach (STNodeOption option in node.GetAllInputOptions())
		{
			if (option != null)
			{
				options.Add(options.Count, option);
			}
		}
		foreach (STNodeOption option in node.GetAllOutputOptions())
		{
			if (option != null)
			{
				options.Add(options.Count, option);
			}
		}
	}

	private static int ReadCount(Stream stream, int maximum, string valueName)
	{
		int count = ReadInt32(stream);
		if (count < 0 || count > maximum)
		{
			throw new InvalidDataException($"{valueName}数量无效：{count}");
		}
		return count;
	}

	private static int ReadInt32(Stream stream)
	{
		return BitConverter.ToInt32(ReadBytes(stream, 4), 0);
	}

	private static byte[] ReadBytes(Stream stream, int count)
	{
		byte[] buffer = new byte[count];
		int offset = 0;
		while (offset < count)
		{
			int read = stream.Read(buffer, offset, count - offset);
			if (read <= 0)
			{
				throw new EndOfStreamException("节点数据意外结束");
			}
			offset += read;
		}
		return buffer;
	}

	private static void WriteInt32(Stream stream, int value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		stream.Write(bytes, 0, bytes.Length);
	}

	private static byte[] DecompressGZip(byte[] data, int offset)
	{
		int compressedLength = data.Length - offset;
		if (compressedLength < 18)
		{
			throw new InvalidDataException("压缩节点数据不完整");
		}
		using MemoryStream input = new MemoryStream(data, offset, compressedLength, writable: false);
		using GZipStream gzip = new GZipStream(input, CompressionMode.Decompress);
		using MemoryStream output = new MemoryStream();
		byte[] buffer = new byte[81920];
		while (true)
		{
			int read = gzip.Read(buffer, 0, buffer.Length);
			if (read <= 0)
			{
				break;
			}
			if (output.Length + read > MaximumDecompressedGraphLength)
			{
				throw new InvalidDataException("解压后的节点数据超过限制");
			}
			output.Write(buffer, 0, read);
		}

		byte[] decompressed = output.ToArray();
		uint expectedCrc = BitConverter.ToUInt32(data, data.Length - 8);
		uint expectedLength = BitConverter.ToUInt32(data, data.Length - 4);
		if (expectedLength != unchecked((uint)decompressed.Length) || expectedCrc != ComputeCrc32(decompressed))
		{
			throw new InvalidDataException("压缩节点数据校验失败");
		}
		return decompressed;
	}

	private static uint ComputeCrc32(byte[] data)
	{
		uint crc = uint.MaxValue;
		foreach (byte value in data)
		{
			crc = Crc32Table[(crc ^ value) & byte.MaxValue] ^ crc >> 8;
		}
		return ~crc;
	}

	private static uint[] CreateCrc32Table()
	{
		uint[] table = new uint[256];
		for (uint i = 0; i < table.Length; i++)
		{
			uint value = i;
			for (int bit = 0; bit < 8; bit++)
			{
				value = (value & 1) != 0 ? 0xEDB88320u ^ value >> 1 : value >> 1;
			}
			table[i] = value;
		}
		return table;
	}
}
