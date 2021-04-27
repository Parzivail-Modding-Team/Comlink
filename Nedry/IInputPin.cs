namespace Nedry
{
	public interface IInputPin : IPin
	{
		bool CanConnectTo(IOutputPin other);
	}
}