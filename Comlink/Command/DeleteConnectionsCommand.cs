using Comlink.Render;
using Nedry;
using SimpleUndoRedo;

namespace Comlink.Command
{
	public class DeleteConnectionsCommand : ICommand<Graph>
	{
		private readonly Connection[] _connections;

		public DeleteConnectionsCommand(Connection[] connections)
		{
			_connections = connections;
		}

		/// <inheritdoc />
		public void Apply(Graph source)
		{
			foreach (var connection in _connections)
				source.RemoveConnection(connection.Source, connection.Destination);
		}

		/// <inheritdoc />
		public void Revert(Graph source)
		{
			foreach (var connection in _connections)
				source.CreateConnection(connection.Source, connection.Destination);
		}
	}
}