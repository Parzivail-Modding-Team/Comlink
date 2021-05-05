using Nedry;

namespace Comlink.Model.Nodes
{
	public class TriggerEventNode : Node
	{
		public TriggerEventNode(string eventName) : base(NodeTypes.TriggerEvent, NodeId.NewId())
		{
			Name = "Set Variable";
			Color = 0xFF_bdb76b;

			InputPins.Add(new FlowInputPin(PinId.NewId(NodeId, PinType.Input, 0)));

			OutputPins.Add(new FlowOutputPin(PinId.NewId(NodeId, PinType.Output, 0), eventName));
		}
	}
}