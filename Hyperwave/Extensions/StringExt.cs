using System;

namespace Hyperwave.Extensions
{
	public static class StringExt
	{
		public static int IndexOf(this string s, Func<char, bool> predicate)
		{
			for (var i = 0; i < s.Length; i++)
				if (predicate(s[i]))
					return i;
			return -1;
		}

		public static int LastIndexOf(this string s, Func<char, bool> predicate)
		{
			for (var i = s.Length - 1; i >= 0; i--)
				if (predicate(s[i]))
					return i;
			return -1;
		}
	}
}