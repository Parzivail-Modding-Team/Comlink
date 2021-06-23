using Hyperwave.Controls;
using Hyperwave.Render;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using SkiaSharp;

namespace Hyperwave
{
	public class NodeRenderingWindow : SkiaWindow
	{
		private readonly TextBoxControl _control;

		/// <inheritdoc />
		public NodeRenderingWindow() : base(new GameWindowSettings { RenderFrequency = 0 },
			new NativeWindowSettings { Size = new Vector2i(960, 540) })
		{
			MouseDown += args => { FocusManager.MouseDown(args, MousePosition); };
			MouseUp += args => { FocusManager.MouseUp(args, MousePosition); };
			MouseMove += FocusManager.MouseMove;
			KeyDown += FocusManager.KeyDown;
			TextInput += FocusManager.TextInput;

			_control = new TextBoxControl
			{
				Transformation = SKMatrix.CreateTranslation(100, 100),
				Size = new SKSize(200, float.NaN),
				Renderer = new DefaultTextBoxRenderer()
			};
			FocusManager.FocusedControl = _control;
		}

		/// <inheritdoc />
		protected override void Draw(SKCanvas canvas)
		{
			_control.Draw(canvas);
		}
	}
}