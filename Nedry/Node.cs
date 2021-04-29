using System;
using System.Collections.Generic;

namespace Nedry
{
	public class Node
	{
		public readonly List<Connection> Connections = new();
		public readonly List<IInputPin> InputPins = new();
		public readonly List<IOutputPin> OutputPins = new();
		public float X { get; set; }
		public float Y { get; set; }
		public Guid NodeType { get; init; }
		public Guid NodeId { get; init; }
		public uint Color { get; set; }
		public string Name { get; set; }

		public Node(Guid nodeType, Guid nodeId)
		{
			NodeType = nodeType;
			NodeId = nodeId;
		}
	}
}