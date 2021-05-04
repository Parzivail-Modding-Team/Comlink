using System;

namespace Nedry
{
	internal class IdHelper
	{
		private static readonly Random Rand = new();

		public static byte[] RandomBytes(int n)
		{
			var bytes = new byte[n];
			Rand.NextBytes(bytes);
			return bytes;
		}

		public static byte[] FromString(string id)
		{
			if (id.Length % 2 != 0)
				throw new ArgumentException("Hex string must be of even length", nameof(id));

			var output = new byte[id.Length / 2];

			for (var i = 0; i < output.Length; i++)
			{
				var highNibble = CharToNibble(id, i * 2);
				var lowNibble = CharToNibble(id, i * 2 + 1);
				output[i] = (byte) ((highNibble << 4) | lowNibble);
			}

			return output;
		}

		public static string ToString(byte[] bytes)
		{
			var output = new char[bytes.Length * 2];

			for (var i = 0; i < bytes.Length; i++)
			{
				var high = bytes[i] >> 4;
				var low = bytes[i] & 0xF;
				output[i * 2] = NibbleToChar(high);
				output[i * 2 + 1] = NibbleToChar(low);
			}

			return new string(output);
		}

		private static byte CharToNibble(string s, int offset)
		{
			// will be 0 for digits, 1 for alpha. 58 = '9' + 1, highest digit char
			var nibbleType = ((s[offset] - 58) >> 31) + 1;

			// 7 = 'A' - '0' - 10, the offset between digit chars and alpha chars
			return (byte) ((s[offset] - '0' - 7 * nibbleType) & 0xF);
		}

		private static char NibbleToChar(int nibble)
		{
			// will be 0 for digits, 1 for alpha
			var nibbleType = ((nibble - 10) >> 31) + 1;

			// 7 = 'A' - '0' - 10, the offset between digit chars and alpha chars
			return (char) ('0' + nibble + nibbleType * 7);
		}
	}
}