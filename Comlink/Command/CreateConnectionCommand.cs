using Comlink.Render;
using Nedry.Pin;

namespace Comlink.Command
{
	public class CreateConnectionCommand : ICommand<Graph>
	{
		private readonly PinId _a;
		private readonly PinId _b;

		public CreateConnectionCommand(PinId a, PinId b)
		{
			_a = a;
			_b = b;
		}

		/// <inheritdoc />
		public void Apply(Graph source)
		{
			source.CreateConnection(_a, _b);
		}

		/// <inheritdoc />
		public void Revert(Graph source)
		{
			source.RemoveConnection(_a, _b);
		}
	}
}