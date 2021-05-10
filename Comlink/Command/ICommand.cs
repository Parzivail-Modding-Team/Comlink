namespace Comlink.Command
{
	public interface ICommand<in T>
	{
		void Apply(T source);
		void Revert(T source);
	}
}