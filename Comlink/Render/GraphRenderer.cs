using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using Nedry;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using SkiaSharp;

namespace Comlink.Render
{
	public class GraphRenderer
	{
		private const SKColorType ColorType = SKColorType.Rgba8888;
		private const GRSurfaceOrigin SurfaceOrigin = GRSurfaceOrigin.BottomLeft;

		private readonly object _bufferSync = new();

		private readonly GLWpfControl _control;
		private readonly NodeRenderer _nodeRenderer;

		private readonly Node _testNode = new()
		{
			Name = "Test Node",
			Color = 0xFF_87cefa,
			InputPins = new IInputPin[] {new FlowInputPin(), new BasicInputPin("In 1"), new BasicInputPin("In 2"), new BasicInputPin("In 3")},
			OutputPins = new IOutputPin[] {new FlowOutputPin(""), new BasicOutputPin("Out 1"), new BasicOutputPin("Out 2")}
		};

		private Vector2 _boardOffset = Vector2.Zero;
		private float _boardZoom = 1;
		private SKCanvas _canvas;
		private GRGlFramebufferInfo _glInfo;

		private GRContext _grContext;

		private Vector2 _lastMousePos = Vector2.Zero;

		private SKSizeI _lastSize;
		private Vector2 _mouseDownPos = Vector2.Zero;
		private GRBackendRenderTarget _renderTarget;
		private SKSizeI _size;
		private SKSurface _surface;

		public GraphRenderer(GLWpfControl control)
		{
			_control = control;

			var baseNodePaint = new SKPaint
			{
				IsAntialias = true,
				TextSize = 16,
				SubpixelText = true,
				LcdRenderText = true,
				IsAutohinted = true
			};
			_nodeRenderer = new NodeRenderer(
				baseNodePaint,
				SKTypeface.FromFamilyName("IBM Plex Sans", SKFontStyleWeight.Medium, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
				SKTypeface.FromFamilyName("IBM Plex Sans", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
			);
		}

		private Vector2 GetRelativeMousePositionOnBoard(MouseEventArgs e)
		{
			var halfScreen = new Vector2(_size.Width / 2f, _size.Height / 2f);
			var pos = e.GetPosition(_control);
			return (new Vector2((float) pos.X, (float) pos.Y) - halfScreen) / _boardZoom + halfScreen;
		}

		private Vector2 GetAbsoluteMousePositionOnBoard(MouseEventArgs e)
		{
			return GetRelativeMousePositionOnBoard(e) - _boardOffset;
		}

		public void OnMouseMove(MouseEventArgs e)
		{
			var posOnBoard = GetRelativeMousePositionOnBoard(e);

			if (e.RightButton == MouseButtonState.Pressed) _boardOffset += posOnBoard - _lastMousePos;

			_lastMousePos = posOnBoard;
		}

		public void OnMouseDown(MouseButtonEventArgs e)
		{
			_lastMousePos = GetRelativeMousePositionOnBoard(e);
			_mouseDownPos = GetAbsoluteMousePositionOnBoard(e);

			// var nodeContains = _nodeRenderer.NodeContains(50, 50, _testNode, _mouseDownPos.X, _mouseDownPos.Y);
		}

		public void OnMouseWheel(MouseWheelEventArgs e)
		{
			if (e.Delta < 0)
				_boardZoom /= 2;
			else
				_boardZoom *= 2;
		}

		public void OnSizeChanged(SizeChangedEventArgs e)
		{
		}

		public void OnRender(TimeSpan dt)
		{
			GL.ClearColor(Color.White);

			var width = Math.Max(_control.FrameBufferWidth, 1);
			var height = Math.Max(_control.FrameBufferHeight, 1);

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

				GL.GetInteger(GetPName.StencilBits, out var stencil);
				GL.GetInteger(GetPName.Samples, out var samples);
				var maxSamples = _grContext.GetMaxSurfaceSampleCount(ColorType);
				if (samples > maxSamples)
					samples = maxSamples;
				_glInfo = new GRGlFramebufferInfo((uint) _control.Framebuffer, ColorType.ToGlSizedFormat());

				// destroy the old surface
				_surface?.Dispose();
				_surface = null;
				_canvas = null;

				// re-create the render target
				_renderTarget?.Dispose();
				_renderTarget = new GRBackendRenderTarget(_size.Width, _size.Height, samples, stencil, _glInfo);
			}

			// create the surface
			if (_surface == null)
			{
				_surface = SKSurface.Create(_grContext, _renderTarget, SurfaceOrigin, ColorType);
				_canvas = _surface.Canvas;
			}

			if (_surface == null || _canvas == null)
				throw new InvalidOperationException();

			// render the canvas
			using (new SKAutoCanvasRestore(_canvas, true))
			{
				_canvas.Clear();
				_canvas.Save();

				_canvas.Translate(_size.Width / 2f, _size.Height / 2f);
				_canvas.Scale(_boardZoom);
				_canvas.Translate(-_size.Width / 2f, -_size.Height / 2f);

				_canvas.Translate(_boardOffset.X, _boardOffset.Y);

				// draw graph

				_nodeRenderer.DrawNode(_canvas, 50, 50, _testNode);

				_canvas.Restore();
				_canvas.Flush();
			}

			var err = GL.GetError();
			if (err != ErrorCode.NoError)
				Debug.WriteLine(err);

			GL.Finish();
		}
	}

	internal class BasicInputPin : IInputPin
	{
		public BasicInputPin(string name)
		{
			Name = name;
		}

		/// <inheritdoc />
		public string Name { get; set; }

		/// <inheritdoc />
		public Guid PinId { get; set; }

		/// <inheritdoc />
		public uint Color { get; set; } = 0xFF_00bfff;

		/// <inheritdoc />
		public Connection CreateConnection(IOutputPin output, IInputPin input)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public bool CanConnectTo(IOutputPin other)
		{
			throw new NotImplementedException();
		}
	}

	internal class BasicOutputPin : IOutputPin
	{
		public BasicOutputPin(string name)
		{
			Name = name;
		}

		/// <inheritdoc />
		public string Name { get; set; }

		/// <inheritdoc />
		public Guid PinId { get; set; }

		/// <inheritdoc />
		public uint Color { get; set; } = 0xFF_32cd32;

		/// <inheritdoc />
		public Connection CreateConnection(IOutputPin output, IInputPin input)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public bool CanConnectTo(IInputPin other)
		{
			throw new NotImplementedException();
		}
	}

	internal class FlowInputPin : BasicInputPin
	{
		/// <inheritdoc />
		public FlowInputPin() : base(string.Empty)
		{
			Color = 0xFF_FFFFFF;
		}
	}

	internal class FlowOutputPin : BasicOutputPin
	{
		/// <inheritdoc />
		public FlowOutputPin(string name) : base(name)
		{
			Color = 0xFF_FFFFFF;
		}
	}
}