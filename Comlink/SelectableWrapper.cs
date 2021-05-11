namespace Comlink
{
	public class SelectableWrapper<T>
	{
		public bool Selected { get; set; }
		public T Value { get; set; }

		public SelectableWrapper(T value)
		{
			Value = value;
		}
	}
}