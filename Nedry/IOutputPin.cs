namespace Nedry
{
	public interface IOutputPin : IPin
	{
		bool CanConnectTo(IInputPin other);
	}
}