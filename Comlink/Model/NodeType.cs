namespace Comlink.Model
{
	public enum NodeType : uint
	{
		Interact = NodeCategory.Control | 0x1,
		Exit = NodeCategory.Control | 0x2,
		Branch = NodeCategory.Flow | 0x1,
		PlayerDialogue = NodeCategory.Dialogue | 0x1,
		NpcDialogue = NodeCategory.Dialogue | 0x2,
		VariableGet = NodeCategory.Interop | 0x1,
		VariableSet = NodeCategory.Interop | 0x2,
		ConstantRead = NodeCategory.Interop | 0x3,
		TriggerEvent = NodeCategory.Interop | 0x4
	}
}