using System.Linq;
using Comlink.Render;
using Nedry;
using Nedry.Pin;
using SimpleUndoRedo;

namespace Comlink.Command
{
	public class SetOutputsAndConnectionsCommand : ICommand<Graph>
	{
		private readonly Connection[] _connections;
		private readonly UniqueId _nodeId;
		private readonly Connection[] _oldConnections;

		private readonly IOutputPin[] _oldOutputPins;
		private readonly IOutputPin[] _outputPins;

		public SetOutputsAndConnectionsCommand(Node node, IOutputPin[] outputPins, Connection[] connections)
		{
			_nodeId = node.NodeId;

			_oldOutputPins = node.OutputPins.ToArray();
			_oldConnections = node.Connections.ToArray();

			_outputPins = outputPins;
			_connections = connections;
		}

		/// <inheritdoc />
		public void Apply(Graph source)
		{
			var node = source.First(comlinkNode => comlinkNode.NodeId == _nodeId);

			node.OutputPins.Clear();
			node.OutputPins.AddRange(_outputPins);

			node.Connections.Clear();
			node.Connections.AddRange(_connections);
		}

		/// <inheritdoc />
		public void Revert(Graph source)
		{
			var node = source.First(comlinkNode => comlinkNode.NodeId == _nodeId);

			node.OutputPins.Clear();
			node.OutputPins.AddRange(_oldOutputPins);

			node.Connections.Clear();
			node.Connections.AddRange(_oldConnections);
		}
	}
}