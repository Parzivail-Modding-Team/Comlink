using System;

namespace Nedry
{
	public class Node
	{
		public Connection[] Connections;
		public IInputPin[] InputPins;
		public IOutputPin[] OutputPins;

		public float X { get; set; }
		public float Y { get; set; }
		public Guid NodeId { get; set; }
		public uint Color { get; set; }
		public string Name { get; set; }
	}
}