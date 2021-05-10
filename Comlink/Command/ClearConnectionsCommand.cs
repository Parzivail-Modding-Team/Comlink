using System.Linq;
using Comlink.Render;
using Nedry;
using Nedry.Pin;

namespace Comlink.Command
{
	public class ClearConnectionsCommand : ICommand<Graph>
	{
		private readonly Connection[] _connections;

		public ClearConnectionsCommand(Graph graph, PinId pin)
		{
			_connections = graph.SelectMany(node => node.Connections.Where(connection => connection.Source == pin || connection.Destination == pin)).ToArray();
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