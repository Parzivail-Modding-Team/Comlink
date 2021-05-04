using System;

namespace Comlink
{
	public class TypeColorConverter
	{
		public static uint GetColor(Type type)
		{
			// TODO
			return (uint) ((type.GetHashCode() & 0xFFFFFF) | 0xFF000000);
		}
	}
}