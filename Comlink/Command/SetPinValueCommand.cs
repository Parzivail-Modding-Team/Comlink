using Comlink.Model;
using Comlink.Render;
using Nedry;
using Nedry.Pin;
using SimpleUndoRedo;

namespace Comlink.Command
{
	public class SetPinValueCommand : ICommand<Graph>
	{
		private readonly string _newValue;
		private readonly UniqueId _nodeId;
		private readonly string _oldValue;
		private readonly PinId _pin;

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