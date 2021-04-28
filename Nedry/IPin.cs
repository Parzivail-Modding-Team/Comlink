using System;

namespace Nedry
{
	public interface IPin
	{
		Guid PinId { get; init; }
		string Name { get; set; }
		uint Color { get; set; }
		bool CanConnectTo(IPin other);
	}
}