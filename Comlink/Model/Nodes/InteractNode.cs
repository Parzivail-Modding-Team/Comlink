using Nedry;
using Nedry.Pin;

namespace Comlink.Model.Nodes
{
	public class InteractNode : ComlinkNode
	{
		public InteractNode() : base(NodeType.Interact, UniqueId.NewId())
		{
			Name = "Interact";
			Color = 0xFF_32cd32;

			OutputPins.Add(new FlowOutputPin(PinId.NewId(NodeId, PinType.Output, 0), string.Empty));
		}
	}
}