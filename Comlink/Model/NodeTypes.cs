using System;

namespace Comlink.Model
{
	public class NodeTypes
	{
		public static readonly Guid Interact = Guid.Parse("d92baa54-392b-4755-b13a-be22f569ddfa");
		public static readonly Guid Exit = Guid.Parse("6dbba347-9553-42eb-b81c-9c137e2a33c2");
		public static readonly Guid Branch = Guid.Parse("76d777aa-8640-4725-a557-f7e7f5efbaf3");
		public static readonly Guid PlayerDialogue = Guid.Parse("fe137d8a-8bf9-4508-8ccc-ee7ebb5ee915");
		public static readonly Guid VariableGet = Guid.Parse("70dd6168-76ea-4baf-a79e-bfa6922249af");
		public static readonly Guid NpcDialogue = Guid.Parse("77ac169a-42e9-4400-abd7-19cba64a5d80");
		public static readonly Guid VariableSet = Guid.Parse("05595073-df39-45d0-8179-33c7d787a063");
		public static readonly Guid TriggerEvent = Guid.Parse("843948ca-9464-41b6-94ba-ec932f304565");
	}
}