using System;

namespace Nedry
{
	public class Connection
	{
		public string Name { get; set; }
		public Guid SourceNodeId { get; }
		public Guid SourcePinId { get; }
		public Guid DestNodeId { get; }
		public Guid DestPinId { get; }

		public Connection(Guid sourceNodeId, Guid sourcePinId, Guid destNodeId, Guid destPinId)
		{
			SourceNodeId = sourceNodeId;
			SourcePinId = sourcePinId;
			DestNodeId = destNodeId;
			DestPinId = destPinId;
		}
	}
}