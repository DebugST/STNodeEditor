using System;

namespace ST.Library.UI.NodeEditor;

public class STNodeConstant
{
	public const byte Version = 1;

	public static byte[] NodeFlag = new byte[4] { 83, 84, 78, 68 };

	public static int NodeFlagInt = BitConverter.ToInt32(NodeFlag, 0);
}
