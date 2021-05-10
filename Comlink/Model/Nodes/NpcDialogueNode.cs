using Nedry;
using Nedry.Pin;

namespace Comlink.Model.Nodes
{
	public class NpcDialogueNode : ComlinkNode
	{
		private string _dialogue;

		public string Dialogue
		{
			get => _dialogue;
			set
			{
				_dialogue = value;
				OutputPins[0] = CreateOutputPin(_dialogue);
			}
		}

		public NpcDialogueNode() : base(NodeType.NpcDialogue, NodeId.NewId())
		{
			Name = "NPC Dialogue";
			Color = 0xFF_9370db;

			InputPins.Add(new FlowInputPin(PinId.NewId(NodeId, PinType.Input, 0)));

			OutputPins.Add(CreateOutputPin(string.Empty));
		}

		private IOutputPin CreateOutputPin(string text)
		{
			return new FlowOutputPin(PinId.NewId(NodeId, PinType.Output, 0), text);
		}
	}
}