using Comlink.Render;
using Nedry;
using Nedry.Pin;

namespace Comlink.Command
{
	public class SetPinValueCommand : ICommand<Graph>
	{
		private readonly UniqueId _nodeId;
		private readonly PinId _pin;
		private readonly string _oldValue;
		private readonly string _newValue;

		public SetPinValueCommand(UniqueId node, PinId pin, string oldValue, string newValue)
		{
			_nodeId = node;
			_pin = pin;
			_oldValue = oldValue;
			_newValue = newValue;
		}

		/// <inheritdoc />
		public void Apply(Graph source)
		{
			source.GetPin(_pin).Pin.Name = _newValue;
		}

		/// <inheritdoc />
		public void Revert(Graph source)
		{
			source.GetPin(_pin).Pin.Name = _oldValue;
		}
	}
}