using System;
using System.Collections.Generic;
using System.Linq;
using Comlink.Model;
using Comlink.Util;
using Nedry;
using Nedry.Pin;

namespace Comlink.Render
{
	public class Graph : List<ComlinkNode>
	{
		public bool HasSource(IPin pin)
		{
			return this.SelectMany(node => node.Connections).Any(connection => connection.Destination == pin.PinId);
		}

		public bool HasDestination(IPin pin)
		{
			return this.SelectMany(node => node.Connections).Any(connection => connection.Source == pin.PinId);
		}

		public void CreateConnection(PinId idA, PinId idB)
		{
			var (nodeA, pinA) = GetPin(idA);
			var (nodeB, pinB) = GetPin(idB);

			if (!pinA.CanConnectTo(pinB))
				return;

			if (pinB is IOutputPin && pinA is IInputPin)
			{
				// b -> a

				if (pinA is TypeInputPin && HasSource(pinA))
					return;

				if (pinB is FlowOutputPin && HasDestination(pinB))
					nodeB.Connections.RemoveAll(connection => connection.Source == pinB.PinId);

				nodeB.Connections.Add(new Connection(pinB.PinId, pinA.PinId));
			}
			else if (pinA is IOutputPin && pinB is IInputPin)
			{
				// a -> b

				if (pinB is TypeInputPin && HasSource(pinB))
					return;

				if (pinA is FlowOutputPin && HasDestination(pinA))
					nodeA.Connections.RemoveAll(connection => connection.Source == pinA.PinId);

				nodeA.Connections.Add(new Connection(pinA.PinId, pinB.PinId));
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		public PinReference GetPin(PinId pinId)
		{
			var node = this.FirstOrDefault(node1 => node1.NodeId == pinId.Node);
			if (node == null)
				return null;

			var pinIdx = pinId.GetIndex();
			IPin pin;

			switch (pinId.GetSide())
			{
				case PinType.Input:
				{
					if (pinIdx >= node.InputPins.Count)
						return null;

					pin = node.InputPins[pinIdx];
					break;
				}
				case PinType.Output:
				{
					if (pinIdx >= node.OutputPins.Count)
						return null;

					pin = node.OutputPins[pinIdx];
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}

			return new PinReference(node, pin);
		}

		public void RemoveConnection(PinId idA, PinId idB)
		{
			var (nodeA, pinA) = GetPin(idA);
			var (nodeB, pinB) = GetPin(idB);

			if (pinB is IOutputPin && pinA is IInputPin)
				// b -> a

				nodeB.Connections.RemoveAll(connection => connection.Source == idB && connection.Destination == idA);
			else if (pinA is IOutputPin && pinB is IInputPin)
				// a -> b

				nodeA.Connections.RemoveAll(connection => connection.Source == idA && connection.Destination == idB);
			else
				throw new InvalidOperationException();
		}
	}
}