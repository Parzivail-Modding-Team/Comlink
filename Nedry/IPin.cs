using System;

namespace Nedry
{
	public interface IPin
	{
		string Name { get; set; }
		Guid PinId { get; set; }
		Connection CreateConnection(IOutputPin output, IInputPin input);
	}
}