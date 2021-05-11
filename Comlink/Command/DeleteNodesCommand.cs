using Comlink.Model;
using Comlink.Render;

namespace Comlink.Command
{
	public class DeleteNodesCommand : ICommand<Graph>
	{
		private readonly ComlinkNode[] _nodes;

		public DeleteNodesCommand(ComlinkNode[] nodes)
		{
			_nodes = nodes;
		}

		/// <inheritdoc />
		public void Apply(Graph source)
		{
			foreach (var node in _nodes)
				source.Remove(node);
		}

		/// <inheritdoc />
		public void Revert(Graph source)
		{
			source.AddRange(_nodes);
		}
	}
}