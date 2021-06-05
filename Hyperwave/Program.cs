using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SkiaSharp;

namespace Hyperwave
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			GLFW.WindowHint(WindowHintInt.StencilBits, 8);
			new NodeRenderingWindow().Run();
		}
	}

	internal class Control
	{
	}

	public class NodeRenderingWindow : SkiaWindow
	{
		/// <inheritdoc />
		public NodeRenderingWindow() : base(new GameWindowSettings {RenderFrequency = 0},
			new NativeWindowSettings {Size = new Vector2i(960, 540)})
		{
		}

		/// <inheritdoc />
		protected override void Draw(SKCanvas canvas)
		{
			using var paint = new SKPaint(new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 36))
			{
				Color = 0xFF_000000,
				IsStroke = false,
				IsAntialias = true,
				HintingLevel = SKPaintHinting.Full,
				LcdRenderText = true
			};

			canvas.DrawText("Hello, World!", 50, 50, paint);
		}
	}
}