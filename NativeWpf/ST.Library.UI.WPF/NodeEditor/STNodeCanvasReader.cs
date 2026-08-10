using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ST.Library.UI.NodeEditor;

/// <summary>
/// Decodes the version-1 STN envelope without mutating a live canvas.
/// Keeping decoding separate from commit makes truncated or corrupt input
/// fail before any node is appended to the currently displayed graph.
/// </summary>
internal static class STNodeCanvasReader
{
    private const int MaximumNodeCount = 10_000;
    private const int MaximumConnectionCount = 100_000;
    private const int MaximumNodeDataLength = 16 * 1024 * 1024;
    private const long MaximumTotalNodeDataLength = 128L * 1024 * 1024;
    private const int MaximumCompressedGraphLength = 160 * 1024 * 1024;
    private const int MaximumDecompressedGraphLength = 160 * 1024 * 1024;
    private static readonly uint[] Crc32Table = CreateCrc32Table();

    internal sealed class Document
    {
        public float CanvasOffsetX { get; init; }
        public float CanvasOffsetY { get; init; }
        public float CanvasScale { get; init; }
        public List<STNode> Nodes { get; } = new();
        public List<Connection> Connections { get; } = new();

        public void ConnectDetachedNodes()
        {
            foreach (Connection connection in Connections)
            {
                STNode outputNode = connection.Output.Owner;
                STNode inputNode = connection.Input.Owner;
                bool outputLocked = outputNode.LockOption;
                bool inputLocked = inputNode.LockOption;
                outputNode.LockOption = false;
                inputNode.LockOption = false;
                try
                {
                    ConnectionStatus status = connection.Output.ConnectOption(
                        connection.Input,
                        isOwnerOfOwner: false);
                    if (status != ConnectionStatus.Connected)
                    {
                        throw new InvalidDataException(
                            $"无法恢复流程连接：{status}");
                    }
                }
                finally
                {
                    outputNode.LockOption = outputLocked;
                    inputNode.LockOption = inputLocked;
                }
            }
        }
    }

    internal sealed class Connection
    {
        public Connection(STNodeOption output, STNodeOption input)
        {
            Output = output;
            Input = input;
        }

        public STNodeOption Output { get; }

        public STNodeOption Input { get; }
    }

    public static Document Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        byte[] header = ReadBytes(stream, STNodeConstant.NodeFlag.Length + 1);
        for (int i = 0; i < STNodeConstant.NodeFlag.Length; i++)
        {
            if (header[i] != STNodeConstant.NodeFlag[i])
                throw new InvalidDataException("无法识别的文件类型");
        }
        if (header[STNodeConstant.NodeFlag.Length] != STNodeConstant.Version)
            throw new InvalidDataException("无法识别的文件版本号");

        byte[] compressed = ReadToEnd(
            stream,
            MaximumCompressedGraphLength);
        byte[] decompressed = DecompressGZip(compressed);
        using var bodyStream = new MemoryStream(
            decompressed,
            writable: false);
        var document = new Document
        {
            CanvasOffsetX = ReadSingle(bodyStream),
            CanvasOffsetY = ReadSingle(bodyStream),
            CanvasScale = ReadSingle(bodyStream),
        };
        if (float.IsNaN(document.CanvasOffsetX)
            || float.IsInfinity(document.CanvasOffsetX)
            || float.IsNaN(document.CanvasOffsetY)
            || float.IsInfinity(document.CanvasOffsetY)
            || float.IsNaN(document.CanvasScale)
            || float.IsInfinity(document.CanvasScale)
            || document.CanvasScale <= 0)
        {
            throw new InvalidDataException("画布视图参数无效");
        }

        int nodeCount = ReadCount(bodyStream, MaximumNodeCount, "节点");
        var options = new Dictionary<long, STNodeOption>();
        var indexedOptions = new HashSet<STNodeOption>();
        long totalNodeDataLength = 0;
        for (int i = 0; i < nodeCount; i++)
        {
            int nodeDataLength = ReadInt32(bodyStream);
            if (nodeDataLength <= 0 || nodeDataLength > MaximumNodeDataLength)
            {
                throw new InvalidDataException(
                    $"第 {i + 1} 个节点数据长度无效：{nodeDataLength}");
            }
            totalNodeDataLength += nodeDataLength;
            if (totalNodeDataLength > MaximumTotalNodeDataLength)
                throw new InvalidDataException("节点数据总长度超过限制");

            STNode node;
            try
            {
                node = CreateNode(ReadBytes(bodyStream, nodeDataLength));
            }
            catch (Exception ex)
            {
                throw new InvalidDataException(
                    $"第 {i + 1} 个节点无法加载",
                    ex);
            }
            document.Nodes.Add(node);
            AddOptions(options, indexedOptions, node);
        }

        int connectionCount = ReadCount(
            bodyStream,
            MaximumConnectionCount,
            "连接");
        var connectionKeys = new HashSet<long>();
        for (int i = 0; i < connectionCount; i++)
        {
            long packed = ReadInt64(bodyStream);
            long outputIndex = packed >> 32;
            long inputIndex = unchecked((uint)packed);
            if (!options.TryGetValue(outputIndex, out STNodeOption output)
                || !options.TryGetValue(inputIndex, out STNodeOption input))
            {
                throw new InvalidDataException(
                    $"第 {i + 1} 条连接引用了不存在的端口");
            }
            if (output.IsInput || !input.IsInput || output.Owner == input.Owner)
            {
                throw new InvalidDataException(
                    $"第 {i + 1} 条连接方向无效");
            }
            if (!connectionKeys.Add(packed))
                throw new InvalidDataException($"第 {i + 1} 条连接重复");

            document.Connections.Add(new Connection(output, input));
        }

        if (bodyStream.ReadByte() != -1)
            throw new InvalidDataException("流程数据包含未识别的尾部内容");

        return document;
    }

    private static byte[] ReadToEnd(Stream stream, int maximumLength)
    {
        using var output = new MemoryStream();
        byte[] buffer = new byte[81_920];
        while (true)
        {
            int read = stream.Read(buffer, 0, buffer.Length);
            if (read <= 0)
                break;
            if (output.Length + read > maximumLength)
                throw new InvalidDataException("压缩流程数据超过限制");
            output.Write(buffer, 0, read);
        }
        return output.ToArray();
    }

    private static byte[] DecompressGZip(byte[] compressed)
    {
        if (compressed.Length < 18)
            throw new InvalidDataException("压缩流程数据不完整");

        using var input = new MemoryStream(compressed, writable: false);
        using var gzip = new GZipStream(
            input,
            CompressionMode.Decompress,
            leaveOpen: true);
        using var output = new MemoryStream();
        byte[] buffer = new byte[81_920];
        while (true)
        {
            int read = gzip.Read(buffer, 0, buffer.Length);
            if (read <= 0)
                break;
            if (output.Length + read > MaximumDecompressedGraphLength)
                throw new InvalidDataException("解压后的流程数据超过限制");
            output.Write(buffer, 0, read);
        }

        byte[] decompressed = output.ToArray();
        uint expectedCrc = BitConverter.ToUInt32(
            compressed,
            compressed.Length - 8);
        uint expectedLength = BitConverter.ToUInt32(
            compressed,
            compressed.Length - 4);
        if (expectedLength != unchecked((uint)decompressed.Length)
            || expectedCrc != ComputeCrc32(decompressed))
        {
            throw new InvalidDataException("压缩流程数据校验失败");
        }
        return decompressed;
    }

    private static uint ComputeCrc32(byte[] data)
    {
        uint crc = uint.MaxValue;
        foreach (byte value in data)
        {
            crc = Crc32Table[(crc ^ value) & byte.MaxValue]
                ^ crc >> 8;
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
                value = (value & 1) != 0
                    ? 0xEDB88320u ^ value >> 1
                    : value >> 1;
            }
            table[i] = value;
        }
        return table;
    }

    private static STNode CreateNode(byte[] data)
    {
        int offset = 0;
        string modelKey = ReadByteLengthString(
            data,
            ref offset,
            "节点类型");
        string typeKey = ReadByteLengthString(
            data,
            ref offset,
            "节点类型标识");
        var properties = new Dictionary<string, byte[]>();
        while (offset < data.Length)
        {
            int keyLength = ReadInt32(data, ref offset, "属性名称长度");
            string propertyName = Encoding.UTF8.GetString(
                ReadBytes(data, ref offset, keyLength, "属性名称"));
            int valueLength = ReadInt32(data, ref offset, "属性值长度");
            byte[] propertyValue = ReadBytes(
                data,
                ref offset,
                valueLength,
                "属性值");
            if (!properties.TryAdd(propertyName, propertyValue))
            {
                throw new InvalidDataException(
                    $"节点数据包含重复属性：{propertyName}");
            }
        }

        STNodeTypeRegistry.TryGetNodeType(
            typeKey,
            modelKey,
            out Type nodeType);
        if (nodeType == null)
        {
            throw new TypeLoadException(
                $"无法找到节点类型 {{{modelKey}}}，请确认对应程序集已加载");
        }

        var node = (STNode)Activator.CreateInstance(nodeType);
        node.OnLoadNode(properties);
        return node;
    }

    private static void AddOptions(
        Dictionary<long, STNodeOption> options,
        HashSet<STNodeOption> indexedOptions,
        STNode node)
    {
        foreach (STNodeOption option in node.GetAllInputOptions())
        {
            if (option != null && indexedOptions.Add(option))
                options.Add(options.Count, option);
        }
        foreach (STNodeOption option in node.GetAllOutputOptions())
        {
            if (option != null && indexedOptions.Add(option))
                options.Add(options.Count, option);
        }
    }

    private static int ReadCount(
        Stream stream,
        int maximum,
        string valueName)
    {
        int count = ReadInt32(stream);
        if (count < 0 || count > maximum)
            throw new InvalidDataException($"{valueName}数量无效：{count}");
        return count;
    }

    private static float ReadSingle(Stream stream)
    {
        return BitConverter.ToSingle(ReadBytes(stream, sizeof(float)), 0);
    }

    private static int ReadInt32(Stream stream)
    {
        return BitConverter.ToInt32(ReadBytes(stream, sizeof(int)), 0);
    }

    private static long ReadInt64(Stream stream)
    {
        return BitConverter.ToInt64(ReadBytes(stream, sizeof(long)), 0);
    }

    private static byte[] ReadBytes(Stream stream, int count)
    {
        byte[] buffer = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int read = stream.Read(buffer, offset, count - offset);
            if (read <= 0)
                throw new EndOfStreamException("流程数据意外结束");
            offset += read;
        }
        return buffer;
    }

    private static string ReadByteLengthString(
        byte[] data,
        ref int offset,
        string valueName)
    {
        if (offset >= data.Length)
            throw new InvalidDataException($"{valueName}缺失");
        int length = data[offset++];
        return Encoding.UTF8.GetString(
            ReadBytes(data, ref offset, length, valueName));
    }

    private static int ReadInt32(
        byte[] data,
        ref int offset,
        string valueName)
    {
        return BitConverter.ToInt32(
            ReadBytes(data, ref offset, sizeof(int), valueName),
            0);
    }

    private static byte[] ReadBytes(
        byte[] data,
        ref int offset,
        int length,
        string valueName)
    {
        if (length < 0 || offset < 0 || offset > data.Length - length)
            throw new InvalidDataException($"{valueName}长度无效：{length}");

        byte[] value = new byte[length];
        Buffer.BlockCopy(data, offset, value, 0, length);
        offset += length;
        return value;
    }
}
