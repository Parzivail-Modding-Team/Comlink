using System;

namespace Nedry
{
	public class Node
	{
		public Connection[] Connections;
		public IInputPin[] InputPins;
		public IOutputPin[] OutputPins;
		public Guid NodeId { get; set; }

		public string Name { get; set; }
	}
}