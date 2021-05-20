using Comlink.Model;
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
			var (node, pin) = source.GetPin(_pin);
			pin.Name = _newValue;

			if (node is ComlinkNode cn)
				cn.MarkWidthDirty();
		}

		/// <inheritdoc />
		public void Revert(Graph source)
		{
			var (node, pin) = source.GetPin(_pin);
			pin.Name = _oldValue;

			if (node is ComlinkNode cn)
				cn.MarkWidthDirty();
		}
	}
}