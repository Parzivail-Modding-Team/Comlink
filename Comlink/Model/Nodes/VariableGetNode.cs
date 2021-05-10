using System;
using Nedry;
using Nedry.Pin;

namespace Comlink.Model.Nodes
{
	public class VariableGetNode : ComlinkNode
	{
		public VariableGetNode(string variable, Type type) : base(NodeType.VariableGet, NodeId.NewId())
		{
			Name = "Read Variable";
			Color = TypeColorConverter.GetColor(type);

			OutputPins.Add(new TypeOutputPin(PinId.NewId(NodeId, PinType.Output, 0), variable, type));
		}
	}
}