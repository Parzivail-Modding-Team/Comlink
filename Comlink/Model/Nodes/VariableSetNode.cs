using Nedry;
using Nedry.Pin;

namespace Comlink.Model.Nodes
{
	public class VariableSetNode : ComlinkNode
	{
		public VariableSetNode(string variable, string type) : base(NodeType.VariableSet, UniqueId.NewId())
		{
			Name = $"Set Variable - {type}";
			Color = HashColorConverter.GetColor(type);

			InputPins.Add(new FlowInputPin(PinId.NewId(NodeId, PinType.Input, 0)));
			InputPins.Add(new TypeInputPin(PinId.NewId(NodeId, PinType.Input, 1), variable, type));

			OutputPins.Add(new FlowOutputPin(PinId.NewId(NodeId, PinType.Output, 0), string.Empty));
		}
	}
}