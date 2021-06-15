using SkiaSharp;

namespace Hyperwave.Controls
{
	internal interface ITransformableContents
	{
		public SKMatrix ContentTransformation { get; set; }
	}
}