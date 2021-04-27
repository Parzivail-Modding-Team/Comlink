using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using Comlink.Render.Shader;
using Nedry;
using OpenTK.Graphics.OpenGL;
using OpenTK.Wpf;
using SkiaSharp;

namespace Comlink.Render
{
	public class GraphRenderer
	{
		private static readonly float Sqrt2 = 1.41421356f;
		private readonly object _bufferSync = new();

		private readonly GLWpfControl _control;

		private readonly IInputPin[] _inputPins = {new FlowInputPin(), new BasicInputPin("In 1"), new BasicInputPin("In 2"), new BasicInputPin("In 3")};
		private readonly IOutputPin[] _outputPins = {new FlowOutputPin(""), new BasicOutputPin("Out 1"), new BasicOutputPin("Out 2")};
		private int _height;
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

		public void OnMouseMove(MouseEventArgs e)
		{
		}

		public void OnMouseDown(MouseButtonEventArgs e)
		{
		}

		public void OnMouseWheel(MouseWheelEventArgs e)
		{
		}

		public void OnSizeChanged(SizeChangedEventArgs e)
		{
			_resizeRequired = true;
		}

		private void DrawNode(SKCanvas ctx, int x, int y, string title, IInputPin[] inputPins, IOutputPin[] outputPins)
		{
			var headerTextPaint = new SKPaint
			{
				Color = new SKColor(0xFF_FFFFFF),
				IsAntialias = true,
				IsStroke = false,
				Typeface = SKTypeface.FromFamilyName("IBM Plex Sans", SKFontStyleWeight.Medium, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
				TextSize = 16,
				SubpixelText = true,
				LcdRenderText = true,
				IsAutohinted = true
			};

			var textPaint = new SKPaint
			{
				Color = new SKColor(0xFF_FFFFFF),
				IsAntialias = true,
				IsStroke = false,
				Typeface = SKTypeface.FromFamilyName("IBM Plex Sans", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
				TextSize = 16,
				SubpixelText = true,
				LcdRenderText = true,
				IsAutohinted = true
			};

			var headerPaint = new SKPaint
			{
				Color = new SKColor(0xFF_9370db),
				IsAntialias = true,
				IsStroke = false
			};

			var boxPaint = new SKPaint
			{
				Color = new SKColor(0xFF_2f4f4f),
				IsAntialias = true,
				IsStroke = false
			};

			var inputPaint = new SKPaint
			{
				Color = new SKColor(0xFF_00bfff),
				IsAntialias = true,
				IsStroke = false
			};

			var outputPaint = new SKPaint
			{
				Color = new SKColor(0xFF_32cd32),
				IsAntialias = true,
				IsStroke = false
			};

			var flowPaint = new SKPaint
			{
				Color = new SKColor(0xFF_FFFFFF),
				IsAntialias = true,
				IsStroke = false
			};

			const int radius = 9;
			const int inset = 3;

			var headerLineHeight = (int) (headerTextPaint.FontMetrics.Descent - headerTextPaint.FontMetrics.Ascent + headerTextPaint.FontMetrics.Leading);
			var headerBaselineOffset = (int) -headerTextPaint.FontMetrics.Ascent;

			var lineHeight = (int) (textPaint.FontMetrics.Descent - textPaint.FontMetrics.Ascent + textPaint.FontMetrics.Leading);

			var width = 200;
			var numPins = Math.Max(inputPins.Length, outputPins.Length);
			var height = lineHeight * (numPins - 1) - textPaint.FontMetrics.Ascent + textPaint.FontMetrics.Descent + 6;

			ctx.Save();
			// ctx.Scale(2);

			// header
			ctx.DrawRoundRect(x - inset, y - headerLineHeight, width + 2 * inset, height + headerLineHeight + inset, radius, radius, headerPaint);

			// title
			ctx.DrawCircle(x + (headerBaselineOffset - inset + 1) / 2f, y - headerLineHeight / 2f, headerLineHeight / 4f, headerTextPaint);
			ctx.DrawText(title, x + headerBaselineOffset, y - headerLineHeight + headerBaselineOffset, headerTextPaint);

			// body
			ctx.DrawRoundRect(x, y, width, height, radius - inset + 1, radius - inset + 1, boxPaint);

			y -= (int) (textPaint.FontMetrics.Ascent - 3);

			for (int i = 0, pY = y; i < inputPins.Length; i++, pY += lineHeight)
			{
				var pin = inputPins[i];

				if (pin is FlowInputPin)
				{
					var r = lineHeight / 10f;
					var l = lineHeight / 4f;
					DrawRoundedTriangle(ctx, x - l / 2, pY - l, l, r + inset, boxPaint);
					DrawRoundedTriangle(ctx, x - l / 2, pY - l, l, r, flowPaint);
				}
				else
				{
					// input dot
					ctx.DrawCircle(x, pY - lineHeight / 4f, lineHeight / 3f, boxPaint);
					ctx.DrawCircle(x, pY - lineHeight / 4f, lineHeight / 3f - inset, inputPaint);
				}

				ctx.DrawText(pin.Name, (int) (x + lineHeight / 2f), pY, textPaint);
			}

			for (int i = 0, pY = y; i < outputPins.Length; i++, pY += lineHeight)
			{
				var pin = outputPins[i];

				if (pin is FlowOutputPin)
				{
					var r = lineHeight / 10f;
					var l = lineHeight / 4f;
					DrawRoundedTriangle(ctx, x + width - l / 2, pY - l, l, r + inset, boxPaint);
					DrawRoundedTriangle(ctx, x + width - l / 2, pY - l, l, r, flowPaint);
				}
				else
				{
					// output dot
					ctx.DrawCircle(x + width, pY - lineHeight / 4f, lineHeight / 3f, boxPaint);
					ctx.DrawCircle(x + width, pY - lineHeight / 4f, lineHeight / 3f - 3, outputPaint);
				}

				var textWidth = textPaint.MeasureText(pin.Name);
				ctx.DrawText(pin.Name, (int) (x + width - textWidth - lineHeight / 2f), pY, textPaint);
			}

			ctx.Restore();
		}

		private static void DrawRoundedTriangle(SKCanvas ctx, float x, float y, float l, float r, SKPaint paint)
		{
			var path = new SKPath();

			var d = r / Sqrt2;

			path.MoveTo(x - r, y);
			path.LineTo(x - r, y - l);

			if (r > 0)
				path.RArcTo(r, r, 135, SKPathArcSize.Small, SKPathDirection.Clockwise, r + d, -d);

			path.LineTo(x + l + d, y - d);

			if (r > 0)
				path.RArcTo(r, r, 90, SKPathArcSize.Small, SKPathDirection.Clockwise, 0, 2 * d);

			path.LineTo(x + d, y + l + d);

			if (r > 0)
				path.RArcTo(r, r, 135, SKPathArcSize.Small, SKPathDirection.Clockwise, -r - d, -d);

			path.Close();

			ctx.DrawPath(path, paint);
		}

		private static void MakeRoundedRect(ref SKPath path, int width, int height, int rTopLeft, int rTopRight, int rBottomRight, int rBottomLeft)
		{
			path.Reset();

			path.MoveTo(width - rTopRight, 0);
			path.RArcTo(rTopRight, rTopRight, 90, SKPathArcSize.Small, SKPathDirection.Clockwise, rTopRight, rTopRight);

			path.LineTo(width, height - rBottomRight);
			path.RArcTo(rBottomRight, rBottomRight, 90, SKPathArcSize.Small, SKPathDirection.Clockwise, -rBottomRight, rBottomRight);

			path.LineTo(rBottomLeft, height);
			path.RArcTo(rBottomLeft, rBottomLeft, 90, SKPathArcSize.Small, SKPathDirection.Clockwise, -rBottomLeft, -rBottomLeft);

			path.LineTo(0, rTopLeft);
			path.RArcTo(rTopLeft, rTopLeft, 90, SKPathArcSize.Small, SKPathDirection.Clockwise, rTopLeft, -rTopLeft);

			path.Close();
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

			// draw graph

			// private readonly IInputPin[] _inputPins = ;
			// private readonly IOutputPin[] _outputPins = {new FlowOutputPin(), new BasicOutputPin("Out 1"), new BasicOutputPin("Out 2")};
			DrawNode(_surface.Canvas, 50, 50, "Node 1",
				new IInputPin[] {new FlowInputPin(), new BasicInputPin("Input Val")},
				new IOutputPin[] {new FlowOutputPin("Route A"), new FlowOutputPin("Route B"), new BasicOutputPin("Bleg")}
			);

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
		}
	}

	internal class FlowOutputPin : BasicOutputPin
	{
		/// <inheritdoc />
		public FlowOutputPin(string name) : base(name)
		{
		}
	}
}