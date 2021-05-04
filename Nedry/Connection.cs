namespace Nedry
{
	public class Connection
	{
		public string Name { get; set; }
		public PinId Source { get; }
		public PinId Destination { get; }

		public Connection(PinId source, PinId destination)
		{
			Source = source;
			Destination = destination;
		}
	}
}