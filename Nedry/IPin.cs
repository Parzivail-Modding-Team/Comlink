namespace Nedry
{
	public interface IPin
	{
		PinId PinId { get; init; }
		string Name { get; set; }
		uint Color { get; set; }
		bool CanConnectTo(IPin other);
	}
}