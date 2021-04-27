using System;

namespace Nedry
{
	public class Connection
	{
		public string Name { get; set; }
		public Guid OutputPinId { get; set; }
		public Guid InputPinId { get; set; }
	}
}