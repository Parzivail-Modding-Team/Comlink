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
		public Guid NodeId { get; set; }
		public uint Color { get; set; }
		public string Name { get; set; }
	}
}