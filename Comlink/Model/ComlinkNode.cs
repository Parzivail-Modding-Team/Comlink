using Nedry;

namespace Comlink.Model
{
	public class ComlinkNode : Node
	{
		public NodeType NodeType { get; init; }

		/// <inheritdoc />
		public ComlinkNode(NodeType type, UniqueId nodeId) : base(nodeId)
		{
			NodeType = type;
		}
	}
}