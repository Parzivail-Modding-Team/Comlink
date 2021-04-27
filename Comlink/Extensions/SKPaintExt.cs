using SkiaSharp;

namespace Comlink.Extensions
{
	// ReSharper disable once InconsistentNaming
	public static class SKPaintExt
	{
		public static SKPaint WithTypeface(this SKPaint paint, SKTypeface typeface)
		{
			paint.Typeface = typeface;
			return paint;
		}

		public static SKPaint WithColor(this SKPaint paint, uint color)
		{
			paint.Color = new SKColor(color);
			return paint;
		}

		public static SKPaint AsFill(this SKPaint paint)
		{
			paint.Style = SKPaintStyle.Fill;
			return paint;
		}

		public static SKPaint AsStroke(this SKPaint paint)
		{
			paint.Style = SKPaintStyle.Stroke;
			return paint;
		}
	}
}