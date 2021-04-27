using SkiaSharp;

namespace Comlink.Extensions
{
	// ReSharper disable once InconsistentNaming
	public static class SKCanvasExt
	{
		private const float Sqrt2 = 1.41421356f;

		public static void DrawRoundedTriangle(this SKCanvas ctx, float x, float y, float l, float r, SKPaint paint)
		{
			var path = new SKPath();

			var d = r / Sqrt2;

			path.MoveTo(x - r, y);
			path.LineTo(x - r, y - l);

			if (r > 0)
				path.RArcTo(r, r, 135, SKPathArcSize.Small, SKPathDirection.Clockwise, r + d, -d);

			path.LineTo(x + l + d, y - d);

			if (r > 0)
				path.RArcTo(r, r, 90, SKPathArcSize.Small, SKPathDirection.Clockwise, 0, 2 * d);

			path.LineTo(x + d, y + l + d);

			if (r > 0)
				path.RArcTo(r, r, 135, SKPathArcSize.Small, SKPathDirection.Clockwise, -r - d, -d);

			path.Close();

			ctx.DrawPath(path, paint);
		}
	}
}