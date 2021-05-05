using System;
using Nedry;

namespace Comlink.Model.Nodes
{
	public class VariableSetNode : Node
	{
		public VariableSetNode(string variable, Type type) : base(NodeTypes.VariableSet, NodeId.NewId())
		{
			Name = "Set Variable";
			Color = TypeColorConverter.GetColor(type);

			InputPins.Add(new FlowInputPin(PinId.NewId(NodeId, PinType.Input, 0)));
			InputPins.Add(new TypeInputPin(PinId.NewId(NodeId, PinType.Input, 1), variable, type));

			OutputPins.Add(new FlowOutputPin(PinId.NewId(NodeId, PinType.Output, 0), string.Empty));
		}
	}
}