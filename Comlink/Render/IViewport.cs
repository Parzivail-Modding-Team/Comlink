using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Comlink.Render
{
	public interface IViewport
	{
		public int Framebuffer { get; }

		public int Width { get; }
		public int Height { get; }

		public bool IsKeyDown(Keys key);
	}
}