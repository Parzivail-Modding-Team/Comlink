using OpenTK.Windowing.GraphicsLibraryFramework;

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
}