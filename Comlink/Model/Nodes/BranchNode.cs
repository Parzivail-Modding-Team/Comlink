using System;
using Nedry;

namespace Comlink.Model.Nodes
{
	public class BranchNode : Node
	{
		private static readonly Guid InputPinCondition = Guid.Parse("dfb10da1-fcd9-43a1-9e7b-bb35dd410b41");
		private static readonly Guid OutputPinTrue = Guid.Parse("15c994e2-d79c-49fe-9990-01adefcf35fa");
		private static readonly Guid OutputPinFalse = Guid.Parse("cdd2bb69-8a03-4618-8f2b-0583b3b3adfc");

		public BranchNode() : base(NodeTypes.Branch, Guid.NewGuid())
		{
			Name = "Branch";
			Color = 0xFF_ff8c00;

			InputPins.Add(new FlowInputPin());
			InputPins.Add(new TypeInputPin(InputPinCondition, "Condition", typeof(bool)));

			OutputPins.Add(new FlowOutputPin(OutputPinTrue, "True"));
			OutputPins.Add(new FlowOutputPin(OutputPinFalse, "False"));
		}
	}
}