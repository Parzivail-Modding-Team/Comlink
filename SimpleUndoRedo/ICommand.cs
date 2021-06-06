namespace SimpleUndoRedo
{
	public interface ICommand<in T>
	{
		void Apply(T source);
		void Revert(T source);
	}
}