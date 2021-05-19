using Nedry.Pin;

namespace Nedry
{
	public class Connection
	{
		public string Name { get; set; } = string.Empty;
		public PinId Source { get; set; }
		public PinId Destination { get; set; }

		public Connection(PinId source, PinId destination)
		{
			Source = source;
			Destination = destination;
		}
	}
}