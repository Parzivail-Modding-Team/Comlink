using System.Collections.ObjectModel;
using Nedry.Pin;

namespace Nedry
{
	public class Node
	{
		public readonly ObservableCollection<Connection> Connections = new();
		public readonly ObservableCollection<IInputPin> InputPins = new();
		public readonly ObservableCollection<IOutputPin> OutputPins = new();
		public float X { get; set; }
		public float Y { get; set; }
		public UniqueId NodeId { get; set; }
		public uint Color { get; set; }
		public string Name { get; set; }

		public Node(UniqueId nodeId)
		{
			NodeId = nodeId;
		}
	}
}