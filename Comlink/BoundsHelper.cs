using System;

namespace Comlink
{
	public static class BoundsHelper
	{
		public static bool RectContainsInclusive(float x, float y, float w, float h, float pX, float pY)
		{
			return pX >= x && pX <= x + w && pY >= y && pY <= y + h;
		}

		public static bool CircleContainsInclusive(float x, float y, float r, float pX, float pY)
		{
			return Math.Pow(pX - x, 2) + Math.Pow(pY - y, 2) <= Math.Pow(r, 2);
		}
	}
}