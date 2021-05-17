using Nedry;
using Nedry.Pin;

namespace Comlink.Model.Nodes
{
	public class TriggerEventNode : ComlinkNode
	{
		public TriggerEventNode(string eventName) : base(NodeType.TriggerEvent, UniqueId.NewId())
		{
			Name = "Trigger Event";
			Color = 0xFF_bdb76b;

			InputPins.Add(new FlowInputPin(PinId.NewId(NodeId, PinType.Input, 0)));

			OutputPins.Add(new FlowOutputPin(PinId.NewId(NodeId, PinType.Output, 0), eventName));
		}
	}
}