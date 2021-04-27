using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using Comlink.Render.Shader;
using Nedry;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using SkiaSharp;

namespace Comlink.Render
{
	public class GraphRenderer
	{
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
		private int _height;

		private Vector2 _lastMousePos = Vector2.Zero;
		private Vector2 _mouseDownPos = Vector2.Zero;

		private IntPtr _pixelDataPtr;
		private bool _ready;
		private bool _resizeRequired = true;
		private SKSurface _surface;
		private int _width;

		public int ScreenVao { get; set; }
		public ShaderProgram ShaderScreen { get; set; }
		public int ScreenTexture { get; set; }

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

		private void Load()
		{
			GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
			GL.PixelStore(PixelStoreParameter.PackAlignment, 1);

			// Set background color
			GL.ClearColor(1, 1, 1, 1);

			ShaderScreen = new ShaderProgram(
				"#version 330 core\nout vec4 FragColor;\nin vec2 TexCoords;\nuniform sampler2D img;\nvoid main()\n{\nvec4 sample=texture(img,vec2(TexCoords.x,1-TexCoords.y));\nvec3 bg=vec3(1.0, 1.0, 1.0);\nFragColor=vec4(sample.rgb*sample.a+(1.0-sample.a)*bg,1.0);\n}",
				"#version 330 core\nlayout (location=0) in vec2 aPos;\nlayout (location=1) in vec2 aTexCoords;\nout vec2 TexCoords;\nvoid main()\n{\ngl_Position=vec4(aPos.x,aPos.y,0.0,1.0);\nTexCoords=aTexCoords;\n} "
			);
			ShaderScreen.Uniforms.SetValue("img", 0);

			CreateScreenVao();

			ScreenTexture = GL.GenTexture();

			// var pixels = Populate(new byte[Size.X * Size.Y * 4], 0xFF);

			GL.BindTexture(TextureTarget.Texture2D, ScreenTexture);
			// GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, Size.X, Size.Y, 0, PixelFormat.Rgba,
			// 	PixelType.UnsignedByte, pixels);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
				(int) TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
				(int) TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
				(int) TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
				(int) TextureWrapMode.ClampToEdge);
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		private void CreateScreenVao()
		{
			float[] quadVertices = {-1, 1, 0, 1, -1, -1, 0, 0, 1, -1, 1, 0, -1, 1, 0, 1, 1, -1, 1, 0, 1, 1, 1, 1};

			ScreenVao = GL.GenVertexArray();
			var screenVbo = GL.GenBuffer();
			GL.BindVertexArray(ScreenVao);
			GL.BindBuffer(BufferTarget.ArrayBuffer, screenVbo);
			GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices,
				BufferUsageHint.StaticDraw);
			GL.EnableVertexAttribArray(0);
			GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
			GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices,
				BufferUsageHint.StaticDraw);
			GL.EnableVertexAttribArray(1);
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindVertexArray(0);
		}

		private void DrawFullscreenQuad()
		{
			GL.BindVertexArray(ScreenVao);
			GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
		}

		private static byte[] Populate(byte[] arr, byte value)
		{
			for (var i = 0; i < arr.Length; i++) arr[i] = value;
			return arr;
		}

		private Vector2 GetRelativeMousePositionOnBoard(MouseEventArgs e)
		{
			var halfScreen = new Vector2(_width / 2f, _height / 2f);
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
			_resizeRequired = true;
		}

		public void OnRender(TimeSpan dt)
		{
			if (!_ready)
			{
				Load();
				_ready = true;
			}

			if (_resizeRequired)
			{
				_surface?.Dispose();

				_width = Math.Max(_control.FrameBufferWidth, 1);
				_height = Math.Max(_control.FrameBufferHeight, 1);

				if (_pixelDataPtr != IntPtr.Zero)
					Marshal.FreeHGlobal(_pixelDataPtr);

				_pixelDataPtr = Marshal.AllocHGlobal(_width * _height * 4);

				_surface = SKSurface.Create(new SKImageInfo(_width, _height));

				_resizeRequired = false;
			}

			const ClearBufferMask bits = ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit;
			// Reset the view
			GL.Clear(bits);

			_surface.Canvas.Clear();
			_surface.Canvas.Save();

			_surface.Canvas.Translate(_width / 2f, _height / 2f);
			_surface.Canvas.Scale(_boardZoom);
			_surface.Canvas.Translate(-_width / 2f, -_height / 2f);

			_surface.Canvas.Translate(_boardOffset.X, _boardOffset.Y);

			// draw graph

			_nodeRenderer.DrawNode(_surface.Canvas, 50, 50, _testNode);

			_surface.Canvas.Restore();
			_surface.Canvas.Flush();

			var image = _surface.Snapshot();

			image.ReadPixels(new SKImageInfo(_width, _height, SKColorType.Rgba8888, SKAlphaType.Unpremul), _pixelDataPtr);

			GL.BindTexture(TextureTarget.Texture2D, ScreenTexture);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, _width, _height, 0, PixelFormat.Rgba,
				PixelType.UnsignedByte, _pixelDataPtr);

			ShaderScreen.Use();
			DrawFullscreenQuad();
			ShaderScreen.Release();

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