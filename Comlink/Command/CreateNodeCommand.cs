using Comlink.Model;
using Comlink.Render;

namespace Comlink.Command
{
	public class CreateNodeCommand : ICommand<Graph>
	{
		private readonly ComlinkNode _node;

		public CreateNodeCommand(ComlinkNode node)
		{
			_node = node;
		}

		/// <inheritdoc />
		public void Apply(Graph source)
		{
			source.Add(_node);
		}

		/// <inheritdoc />
		public void Revert(Graph source)
		{
			source.Remove(_node);
		}
	}
}