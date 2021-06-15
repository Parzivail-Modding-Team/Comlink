using System;

namespace Hyperwave.Helper
{
	public class TextUtil
	{
		public static int BinarySearchCursor(Func<int, float> widthProvider, int stringLength, float xOffset)
		{
			float Width(int index)
			{
				if (index < 0)
					return 0;
				return index >= stringLength ? widthProvider(stringLength) : widthProvider(index);
			}

			var min = 0;
			var max = stringLength;
			while (min <= max)
			{
				var mid = (min + max) / 2;

				var underSize = Width(mid - 1);
				var overSize = Width(mid);

				if (underSize <= xOffset && overSize > xOffset)
				{
					if (xOffset > (overSize + underSize) / 2)
						return mid;
					return mid - 1;
				}

				if (xOffset < Width(mid))
					max = mid - 1;
				else
					min = mid + 1;
			}

			return xOffset > Width(stringLength) ? stringLength : 0;
		}
	}
}