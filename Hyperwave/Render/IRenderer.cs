using Hyperwave.Controls;
using SkiaSharp;

namespace Hyperwave.Render
{
	public interface IRenderer<in T> where T : Control
	{
		public void Draw(T control, SKCanvas canvas);
	}
}