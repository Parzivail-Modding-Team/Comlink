using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using SkiaSharp;

namespace Hyperwave
{
	public abstract class SkiaWindow : GameWindow
	{
		private const SKColorType ColorType = SKColorType.Rgba8888;

		private static readonly DebugProc DebugCallback = OnGlMessage;
		private const GRSurfaceOrigin SurfaceOrigin = GRSurfaceOrigin.BottomLeft;

		private readonly Stopwatch _frameTimer = new();
		private readonly Stopwatch _renderTimer = new();
		private SKCanvas _canvas;
		private GRGlFramebufferInfo _glInfo;
		private GRContext _grContext;
		private SKSizeI _lastSize;
		private GRBackendRenderTarget _renderTarget;

		private SKSizeI _size;

		private SKSurface _surface;
		public double FrameTime { get; private set; }
		public double MinRenderTime { get; private set; }

		public SkiaWindow(GameWindowSettings g, NativeWindowSettings n) : base(g, n)
		{
			Load += WindowLoad;
			Resize += WindowResize;

			RenderFrame += WindowRender;
			UpdateFrame += WindowUpdate;

			Closing += WindowClosing;
		}

		protected abstract void Draw(SKCanvas canvas);

		private static void OnGlMessage(DebugSource source, DebugType type, int id, DebugSeverity severity, int length,
			IntPtr message, IntPtr userparam)
		{
			if (severity == DebugSeverity.DebugSeverityNotification)
				return;

			var msg = Marshal.PtrToStringAnsi(message, length);
			Console.WriteLine(msg);
		}

		private void WindowClosing(CancelEventArgs obj)
		{
			// Without this the app thread would never abort.
			Environment.Exit(0);
		}

		private void WindowLoad()
		{
			VSync = VSyncMode.On;
			WindowBorder = WindowBorder.Fixed;

			// Set up caps
			GL.Enable(EnableCap.RescaleNormal);
			GL.Enable(EnableCap.DebugOutput);
			GL.DebugMessageCallback(DebugCallback, IntPtr.Zero);
			GL.ActiveTexture(TextureUnit.Texture0);

			// Set background color
			GL.ClearColor(1, 1, 1, 1);
		}

		private void WindowRender(FrameEventArgs e)
		{
			FrameTime = _frameTimer.Elapsed.TotalMilliseconds;
			_frameTimer.Restart();
			_renderTimer.Restart();

			const ClearBufferMask bits = ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit |
			                             ClearBufferMask.StencilBufferBit;
			// Reset the view
			GL.Clear(bits);


			var width = Math.Max(Size.X, 1);
			var height = Math.Max(Size.Y, 1);

			// get the new surface size
			_size = new SKSizeI(width, height);

			// create the contexts if not done already
			if (_grContext == null)
			{
				var glInterface = GRGlInterface.Create();
				_grContext = GRContext.CreateGl(glInterface);
			}

			// manage the drawing surface
			if (_renderTarget == null || _lastSize != _size || !_renderTarget.IsValid)
			{
				// create or update the dimensions
				_lastSize = _size;

				const int fboId = 0;

				// GL.GetInteger(GetPName.StencilBits, out var stencil);
				GL.GetInteger(GetPName.Samples, out var samples);
				var maxSamples = _grContext.GetMaxSurfaceSampleCount(ColorType);
				if (samples > maxSamples)
					samples = maxSamples;
				_glInfo = new GRGlFramebufferInfo(fboId, ColorType.ToGlSizedFormat());

				// destroy the old surface
				_surface?.Dispose();
				_surface = null;
				_canvas = null;

				// re-create the render target
				_renderTarget?.Dispose();
				_renderTarget = new GRBackendRenderTarget(_size.Width, _size.Height, samples, 8, _glInfo);
			}

			// create the surface
			if (_surface == null)
			{
				_surface = SKSurface.Create(_grContext, _renderTarget, SurfaceOrigin, ColorType);
				_canvas = _surface.Canvas;
			}

			if (_surface == null || _canvas == null)
				throw new InvalidOperationException();

			_canvas.Clear(SKColors.White);

			// render the canvas
			using (new SKAutoCanvasRestore(_canvas, true))
			{
				Draw(_canvas);
			}

			_canvas.Flush();

			var err = GL.GetError();
			if (err != ErrorCode.NoError)
				Debug.WriteLine(err);

			_renderTimer.Stop();
			MinRenderTime = _renderTimer.Elapsed.TotalMilliseconds;

			// Swap the graphics buffer
			SwapBuffers();
		}

		private void WindowResize(ResizeEventArgs obj)
		{
			GL.Viewport(0, 0, Size.X, Size.Y);
		}

		private void WindowUpdate(FrameEventArgs e)
		{
			// Title = $"{FrameTime:F1} ms/f, {RenderTime:F2}ms/render";
		}
	}
}