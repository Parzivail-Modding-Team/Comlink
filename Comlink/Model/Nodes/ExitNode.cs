using Nedry;

namespace Comlink.Model.Nodes
{
	public class ExitNode : Node
	{
		public ExitNode() : base(NodeTypes.Exit, NodeId.NewId())
		{
			Name = "Exit";
			Color = 0xFF_cd5c5c;

			InputPins.Add(new FlowInputPin(PinId.NewId(NodeId, PinType.Input, 0)));
		}
	}
}