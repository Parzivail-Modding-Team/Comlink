using OpenTK.Mathematics;
using SkiaSharp;

namespace Hyperwave.Extensions
{
	public static class SKMatrixExt
	{
		public static Vector2 MapPoint(this SKMatrix mat, Vector2 point)
		{
			var mapped = mat.MapPoint(point.X, point.Y);
			return new Vector2(mapped.X, mapped.Y);
		}
	}
}