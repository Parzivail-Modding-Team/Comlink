using System;
using Nedry;

namespace Comlink.Model.Nodes
{
	public class ExitNode : Node
	{
		private static readonly Guid InputPinExit = Guid.Parse("4cd76e82-8dc5-4f06-8d40-ca71d49a045f");

		public ExitNode() : base(NodeTypes.Exit, Guid.NewGuid())
		{
			Name = "Exit";
			Color = 0xFF_cd5c5c;

			InputPins.Add(new FlowInputPin(InputPinExit));
		}
	}
}