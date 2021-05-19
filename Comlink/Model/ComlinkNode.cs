using System;
using System.Collections.Generic;
using System.IO;
using Comlink.Extensions;
using Nedry;
using Nedry.Pin;

namespace Comlink.Model
{
	public class ComlinkNode : Node
	{
		public NodeType NodeType { get; }

		/// <inheritdoc />
		public ComlinkNode(NodeType type, UniqueId nodeId) : base(nodeId)
		{
			NodeType = type;
		}

		public ComlinkNode(NodeType nodeType, UniqueId id, float x, float y, uint color, string name, IEnumerable<IInputPin> inputPins, IEnumerable<IOutputPin> outputPins,
			IEnumerable<Connection> connections) : this(nodeType, id)
		{
			X = x;
			Y = y;
			Color = color;
			Name = name;

			InputPins.AddRange(inputPins);
			OutputPins.AddRange(outputPins);
			Connections.AddRange(connections);
		}

		public void Serialize(BinaryWriter stream)
		{
			stream.Write((uint) NodeType);
			stream.Write(NodeId);
			stream.Write(X);
			stream.Write(Y);
			stream.Write(Color);
			stream.Write(Name);

			stream.Write(InputPins.Count);

			foreach (var pin in InputPins)
			{
				stream.Write((byte) (pin switch
				{
					FlowInputPin _ => SerializedPinType.FlowInputPin,
					TypeInputPin _ => SerializedPinType.TypeInputPin,
					_ => throw new InvalidOperationException()
				}));

				stream.Write(pin.PinId);

				stream.Write(pin.Color);
				stream.Write(pin.Name);

				if (pin is TypeInputPin tip)
					stream.Write(tip.Type);
			}

			stream.Write(OutputPins.Count);

			foreach (var pin in OutputPins)
			{
				stream.Write((byte) (pin switch
				{
					FlowOutputPin _ => SerializedPinType.FlowOutputPin,
					TypeOutputPin _ => SerializedPinType.TypeOutputPin,
					_ => throw new InvalidOperationException()
				}));

				stream.Write(pin.PinId);

				stream.Write(pin.Color);
				stream.Write(pin.Name);

				if (pin is TypeOutputPin tip)
					stream.Write(tip.Type);
			}

			stream.Write(Connections.Count);

			foreach (var connection in Connections)
			{
				stream.Write(connection.Name);
				stream.Write(connection.Source);
				stream.Write(connection.Destination);
			}
		}

		public static ComlinkNode Deserialize(BinaryReader stream)
		{
			var nodeType = (NodeType) stream.ReadUInt32();
			var id = stream.ReadUniqueId();
			var x = stream.ReadSingle();
			var y = stream.ReadSingle();
			var color = stream.ReadUInt32();
			var name = stream.ReadString();

			var numInputPins = stream.ReadInt32();
			var inputPins = new IInputPin[numInputPins];

			for (var i = 0; i < numInputPins; i++)
			{
				var pinType = (SerializedPinType) stream.ReadByte();
				var pinId = stream.ReadPinId();

				var pinColor = stream.ReadUInt32();
				var pinName = stream.ReadString();

				switch (pinType)
				{
					case SerializedPinType.FlowInputPin:
						inputPins[i] = new FlowInputPin(pinId)
						{
							Color = pinColor,
							Name = pinName
						};
						break;
					case SerializedPinType.TypeInputPin:
					{
						var type = stream.ReadString();

						inputPins[i] = new TypeInputPin(pinId, pinName, type)
						{
							Color = pinColor
						};
						break;
					}
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			var numOutputPins = stream.ReadInt32();
			var outputPins = new IOutputPin[numOutputPins];

			for (var i = 0; i < numOutputPins; i++)
			{
				var pinType = (SerializedPinType) stream.ReadByte();
				var pinId = stream.ReadPinId();

				var pinColor = stream.ReadUInt32();
				var pinName = stream.ReadString();

				switch (pinType)
				{
					case SerializedPinType.FlowOutputPin:
						outputPins[i] = new FlowOutputPin(pinId, pinName)
						{
							Color = pinColor
						};
						break;
					case SerializedPinType.TypeOutputPin:
					{
						var type = stream.ReadString();

						outputPins[i] = new TypeOutputPin(pinId, pinName, type)
						{
							Color = pinColor
						};
						break;
					}
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			var numConnections = stream.ReadInt32();
			var connections = new Connection[numConnections];

			for (var i = 0; i < numConnections; i++)
			{
				var cnName = stream.ReadString();
				var cnSrc = stream.ReadPinId();
				var cnDest = stream.ReadPinId();

				connections[i] = new Connection(cnSrc, cnDest)
				{
					Name = cnName
				};
			}

			return new ComlinkNode(nodeType, id, x, y, color, name, inputPins, outputPins, connections);
		}
	}
}