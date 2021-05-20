using System;
using Comlink.Model;
using Comlink.Render;
using Nedry.Pin;

namespace Comlink.Command
{
	public class SetPinTypeValueCommand : ICommand<Graph>
	{
		private readonly PinId _pin;
		private readonly string _oldValue;
		private readonly string _newValue;
		private readonly string _oldType;
		private readonly string _newType;

		public SetPinTypeValueCommand(PinId pin, string oldValue, string newValue, string oldType, string newType)
		{
			_pin = pin;
			_oldValue = oldValue;
			_newValue = newValue;
			_oldType = oldType;
			_newType = newType;
		}

		/// <inheritdoc />
		public void Apply(Graph source)
		{
			var (node, pin) = source.GetPin(_pin);
			if (pin is not ITypedPin typedPin)
				throw new InvalidOperationException();

			pin.Name = _newValue;
			typedPin.Type = _newType;

			node.Color = pin.Color;

			if (node is ComlinkNode cn)
				cn.MarkWidthDirty();
		}

		/// <inheritdoc />
		public void Revert(Graph source)
		{
			var (node, pin) = source.GetPin(_pin);
			if (pin is not ITypedPin typedPin)
				throw new InvalidOperationException();

			pin.Name = _oldValue;
			typedPin.Type = _oldType;

			node.Color = pin.Color;

			if (node is ComlinkNode cn)
				cn.MarkWidthDirty();
		}
	}
}