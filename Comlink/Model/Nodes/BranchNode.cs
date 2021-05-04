﻿using Nedry;

namespace Comlink.Model.Nodes
{
	public class BranchNode : Node
	{
		public BranchNode() : base(NodeTypes.Branch, NodeId.NewId())
		{
			Name = "Branch";
			Color = 0xFF_ff8c00;

			InputPins.Add(new FlowInputPin(PinId.NewId(NodeId, PinType.Input, 0)));
			InputPins.Add(new TypeInputPin(PinId.NewId(NodeId, PinType.Input, 1), "Condition", typeof(bool)));

			OutputPins.Add(new FlowOutputPin(PinId.NewId(NodeId, PinType.Output, 0), "True"));
			OutputPins.Add(new FlowOutputPin(PinId.NewId(NodeId, PinType.Output, 1), "False"));
		}
	}
}