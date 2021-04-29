using System;
using Nedry;

namespace Comlink.Model.Nodes
{
	public class InteractNode : Node
	{
		private static readonly Guid OutputPinInteract = Guid.Parse("bd9cd544-c556-4d0b-bf7a-684d2cd5b3c1");

		public InteractNode() : base(NodeTypes.Interact, Guid.NewGuid())
		{
			Name = "Interact";
			Color = 0xFF_32cd32;

			OutputPins.Add(new FlowOutputPin(OutputPinInteract, string.Empty));
		}
	}
}