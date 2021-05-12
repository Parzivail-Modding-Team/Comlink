using Nedry;
using Nedry.Pin;

namespace Comlink.Model.Nodes
{
	public class ExitNode : ComlinkNode
	{
		public ExitNode() : base(NodeType.Exit, UniqueId.NewId())
		{
			Name = "Exit";
			Color = 0xFF_cd5c5c;

			InputPins.Add(new FlowInputPin(PinId.NewId(NodeId, PinType.Input, 0)));
		}
	}
}