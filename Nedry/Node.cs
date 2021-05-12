using System.Collections.Generic;
using Nedry.Pin;

namespace Nedry
{
	public class Node
	{
		public readonly List<Connection> Connections = new();
		public readonly List<IInputPin> InputPins = new();
		public readonly List<IOutputPin> OutputPins = new();
		public float X { get; set; }
		public float Y { get; set; }
		public UniqueId NodeId { get; init; }
		public uint Color { get; set; }
		public string Name { get; set; }

		public Node(UniqueId nodeId)
		{
			NodeId = nodeId;
		}
	}
}