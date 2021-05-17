namespace Nedry
{
	public class HashColorConverter
	{
		public static uint GetColor(string s)
		{
			var hashedValue = 618258791u;
			foreach (var t in s)
			{
				hashedValue += t;
				hashedValue *= 618258791u;
			}

			// Not using GetHashCode because it isn't repeatable between runs
			return (hashedValue & 0xFFFFFF) | 0xFF000000;
		}
	}
}