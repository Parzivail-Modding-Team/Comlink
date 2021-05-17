using Nedry;
using Nedry.Pin;

namespace Comlink.Model.Nodes
{
	public class ConstantReadNode : ComlinkNode
	{
		public ConstantReadNode(string serializedValue, string type) : base(NodeType.ConstantRead, UniqueId.NewId())
		{
			Name = "Constant";
			Color = HashColorConverter.GetColor(type);

			OutputPins.Add(new TypeOutputPin(PinId.NewId(NodeId, PinType.Output, 0), serializedValue, type));
		}
	}
}