using Nedry;
using Nedry.Pin;

namespace Comlink.Model.Nodes
{
	public class VariableGetNode : ComlinkNode
	{
		public VariableGetNode(string variable, string type) : base(NodeType.VariableGet, UniqueId.NewId())
		{
			Name = "Read Variable";
			Color = HashColorConverter.GetColor(type);

			OutputPins.Add(new TypeOutputPin(PinId.NewId(NodeId, PinType.Output, 0), variable, type));
		}
	}
}