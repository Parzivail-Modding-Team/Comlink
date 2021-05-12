using Nedry;
using Nedry.Pin;

namespace Comlink.Model.Nodes
{
	public class PlayerDialogueNode : ComlinkNode
	{
		public PlayerDialogueNode() : base(NodeType.PlayerDialogue, UniqueId.NewId())
		{
			Name = "Player Dialogue";
			Color = 0xFF_87cefa;

			InputPins.Add(new FlowInputPin(PinId.NewId(NodeId, PinType.Input, 0)));
		}
	}
}