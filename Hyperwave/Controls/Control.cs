using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using SkiaSharp;

namespace Hyperwave.Controls
{
	public class Control
	{
		public SKSize Size { get; set; }
		public SKMatrix Transformation { get; set; }

		public virtual void ConsumeKeyDown(KeyboardKeyEventArgs args)
		{
		}

		public virtual void ConsumeMouseDown(MouseButtonEventArgs args, Vector2 mousePosition)
		{
		}

		public virtual void ConsumeMouseMove(MouseMoveEventArgs args)
		{
		}

		public virtual void ConsumeMouseUp(MouseButtonEventArgs args, Vector2 mousePosition)
		{
		}

		public virtual void ConsumeTextInput(TextInputEventArgs args)
		{
		}

		public virtual void Draw(SKCanvas canvas)
		{
		}
	}
}