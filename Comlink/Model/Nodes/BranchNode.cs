using Nedry;
using Nedry.Pin;

namespace Comlink.Model.Nodes
{
	public class BranchNode : ComlinkNode
	{
		public BranchNode() : base(NodeType.Branch, UniqueId.NewId())
		{
			Name = "Branch";
			Color = 0xFF_ff8c00;

			InputPins.Add(new FlowInputPin(PinId.NewId(NodeId, PinType.Input, 0)));
			InputPins.Add(new TypeInputPin(PinId.NewId(NodeId, PinType.Input, 1), "Condition", "Z"));

			OutputPins.Add(new FlowOutputPin(PinId.NewId(NodeId, PinType.Output, 0), "True"));
			OutputPins.Add(new FlowOutputPin(PinId.NewId(NodeId, PinType.Output, 1), "False"));
		}
	}
}