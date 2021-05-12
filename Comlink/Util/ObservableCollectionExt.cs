using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Comlink.Util
{
	public static class ObservableCollectionExt
	{
		public static int RemoveAll<T>(this ObservableCollection<T> coll, Func<T, bool> condition)
		{
			var itemsToRemove = coll.Where(condition).ToArray();

			foreach (var itemToRemove in itemsToRemove)
				coll.Remove(itemToRemove);

			return itemsToRemove.Length;
		}
	}
}