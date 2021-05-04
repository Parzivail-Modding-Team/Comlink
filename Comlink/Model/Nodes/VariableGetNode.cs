using System;
using Nedry;

namespace Comlink.Model.Nodes
{
	public class VariableGetNode : Node
	{
		public VariableGetNode(string variable, Type type) : base(NodeTypes.VariableGet, NodeId.NewId())
		{
			Name = "Read Variable";
			Color = TypeColorConverter.GetColor(type);

			OutputPins.Add(new TypeOutputPin(PinId.NewId(NodeId, PinType.Output, 0), variable, type));
		}
	}
}