using Nedry;
using Nedry.Pin;

namespace Comlink.Model.Nodes
{
	public class NpcDialogueNode : ComlinkNode
	{
		public NpcDialogueNode() : base(NodeType.NpcDialogue, UniqueId.NewId())
		{
			Name = "NPC Dialogue";
			Color = 0xFF_9370db;

			InputPins.Add(new FlowInputPin(PinId.NewId(NodeId, PinType.Input, 0)));

			OutputPins.Add(new FlowOutputPin(PinId.NewId(NodeId, PinType.Output, 0), string.Empty));
		}
	}
}