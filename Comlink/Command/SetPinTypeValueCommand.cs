using System;
using System.Linq;
using Comlink.Render;
using Nedry;
using Nedry.Pin;

namespace Comlink.Command
{
	public class SetPinTypeValueCommand : ICommand<Graph>
	{
		private readonly UniqueId _nodeId;
		private readonly PinId _pin;
		private readonly string _oldValue;
		private readonly string _newValue;
		private readonly string _oldType;
		private readonly string _newType;

		public SetPinTypeValueCommand(UniqueId node, PinId pin, string oldValue, string newValue, string oldType, string newType)
		{
			_nodeId = node;
			_pin = pin;
			_oldValue = oldValue;
			_newValue = newValue;
			_oldType = oldType;
			_newType = newType;
		}

		/// <inheritdoc />
		public void Apply(Graph source)
		{
			var pin = source.GetPin(_pin).Pin;
			if (pin is not ITypedPin typedPin)
				throw new InvalidOperationException();

			pin.Name = _newValue;
			typedPin.Type = _newType;

			var node = source.First(comlinkNode => comlinkNode.NodeId == _nodeId);
			node.Color = pin.Color;
		}

		/// <inheritdoc />
		public void Revert(Graph source)
		{
			var pin = source.GetPin(_pin).Pin;
			if (pin is not ITypedPin typedPin)
				throw new InvalidOperationException();

			pin.Name = _oldValue;
			typedPin.Type = _oldType;

			var node = source.First(comlinkNode => comlinkNode.NodeId == _nodeId);
			node.Color = pin.Color;
		}
	}
}