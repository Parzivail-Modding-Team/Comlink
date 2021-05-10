using Nedry;

namespace Comlink.Model
{
	public class ComlinkNode : Node
	{
		public NodeType NodeType { get; init; }

		/// <inheritdoc />
		public ComlinkNode(NodeType type, NodeId nodeId) : base(nodeId)
		{
			NodeType = type;
		}
	}
}