using System.IO;
using Nedry;
using Nedry.Pin;

namespace Comlink.Extensions
{
	public static class BinaryWriterExt
	{
		public static void Write(this BinaryWriter stream, UniqueId id)
		{
			var bytes = id.GetBytes();
			stream.Write(bytes.Length);
			stream.Write(bytes);
		}

		public static void Write(this BinaryWriter stream, PinId id)
		{
			stream.Write(id.Node);

			var bytes = id.GetPinBytes();
			stream.Write(bytes.Length);
			stream.Write(bytes);
		}
	}

	public static class BinaryReaderExt
	{
		public static UniqueId ReadUniqueId(this BinaryReader stream)
		{
			var numBytes = stream.ReadInt32();
			return new UniqueId(stream.ReadBytes(numBytes));
		}

		public static PinId ReadPinId(this BinaryReader stream)
		{
			var nodeId = stream.ReadUniqueId();

			var numBytes = stream.ReadInt32();
			var bytes = stream.ReadBytes(numBytes);

			return new PinId(nodeId, bytes);
		}
	}
}