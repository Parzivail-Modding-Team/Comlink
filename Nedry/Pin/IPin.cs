namespace Nedry.Pin
{
	public interface IPin
	{
		PinId PinId { get; set; }
		string Name { get; set; }
		uint Color { get; set; }
		bool CanConnectTo(IPin other);
	}
}