using Nedry;

namespace Comlink
{
	public class KeyedSelectableWrapper<T>
	{
		public bool Selected { get; set; }
		public UniqueId Id { get; }
		public T Value { get; set; }

		public KeyedSelectableWrapper(UniqueId id, T value)
		{
			Id = id;
			Value = value;
		}
	}
}