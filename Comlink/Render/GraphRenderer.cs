﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

		private SKMatrix _boardTransform = SKMatrix.Identity;
		private SKCanvas _canvas;
		private GRGlFramebufferInfo _glInfo;
		private GRContext _grContext;
		private Vector2 _lastMouseBoardPos = Vector2.Zero;
		private Vector2 _lastMouseControlPos = Vector2.Zero;
		private SKSizeI _lastSize;
		private Vector2 _mouseDownPos = Vector2.Zero;

		private readonly List<Node> _nodes = new()
		{
			new()
			{
				Name = "Test Node",
				Color = 0xFF_87cefa,
				InputPins = new IInputPin[] {new FlowInputPin(), new BasicInputPin("In 1"), new BasicInputPin("In 2"), new BasicInputPin("In 3")},
				OutputPins = new IOutputPin[] {new FlowOutputPin(""), new BasicOutputPin("Out 1"), new BasicOutputPin("Out 2")},
				X = 10,
				Y = 50
			},
			new()
			{
				Name = "Test Node 2",
				Color = 0xFF_32cd32,
				InputPins = new IInputPin[] {new FlowInputPin(), new BasicInputPin("In 1"), new BasicInputPin("In 2"), new BasicInputPin("In 3")},
				OutputPins = new IOutputPin[] {new FlowOutputPin(""), new BasicOutputPin("Out 1"), new BasicOutputPin("Out 2")},
				X = 300,
				Y = 50
			}
		};

		private bool _rectangleSelecting;
		private GRBackendRenderTarget _renderTarget;

		private readonly List<Node> _selectedNodes = new();
		private readonly List<Node> _selectedNodesQueue = new();

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

		private Vector2 GetMousePositionOnBoard(MouseEventArgs e)
		{
			var pos = e.GetPosition(_control);
			var transformedPoint = _boardTransform.Invert().MapPoint((float) pos.X, (float) pos.Y);
			return new Vector2(transformedPoint.X, transformedPoint.Y);
		}

		public void OnMouseMove(MouseEventArgs e)
		{
			var controlPos = e.GetPosition(_control);
			var posOnControl = new Vector2((float) controlPos.X, (float) controlPos.Y);

			var delta = posOnControl - _lastMouseControlPos;
			var (dX, dY) = delta / _boardTransform.MapRadius(1);

			_lastMouseControlPos = posOnControl;

			var posOnBoard = GetMousePositionOnBoard(e);
			var node = GetHotNode();

			_lastMouseBoardPos = posOnBoard;

			if (_rectangleSelecting)
			{
				SelectAllInSelectionRectangle();
			}
			else
			{
				if (e.LeftButton == MouseButtonState.Pressed)
				{
					if (node != null)
						foreach (var selectedNode in _selectedNodes)
						{
							selectedNode.X += dX;
							selectedNode.Y += dY;
						}
				}
				else if (e.RightButton == MouseButtonState.Pressed)
				{
					_boardTransform = _boardTransform.PostConcat(SKMatrix.CreateTranslation(delta.X, delta.Y));
				}
			}
		}

		private Node GetHotNode()
		{
			return _nodes.FirstOrDefault(node => _nodeRenderer.GetBounds(node).Contains(new Vector2(_lastMouseBoardPos.X, _lastMouseBoardPos.Y)));
		}

		private bool IsSelected(Node node)
		{
			return _selectedNodes.Contains(node) || _selectedNodesQueue.Contains(node);
		}

		private void SelectNode(Node node)
		{
			if (IsSelected(node))
				return;

			_selectedNodes.Add(node);
		}

		private void SelectAllInSelectionRectangle()
		{
			var selectionRect = new Box2(_mouseDownPos.X, _mouseDownPos.Y, _lastMouseBoardPos.X, _lastMouseBoardPos.Y);

			_selectedNodesQueue.Clear();
			foreach (var node in _nodes.Where(node => !_selectedNodes.Contains(node)))
				if (_nodeRenderer.GetBounds(node).Contains(selectionRect))
					_selectedNodesQueue.Add(node);
		}

		public void OnMouseUp(MouseButtonEventArgs e)
		{
			_rectangleSelecting = false;
			CommitSelectionQueue();
		}

		private void CommitSelectionQueue()
		{
			_selectedNodes.AddRange(_selectedNodesQueue);
			_selectedNodesQueue.Clear();
		}

		public void OnMouseDown(MouseButtonEventArgs e)
		{
			_mouseDownPos = _lastMouseBoardPos = GetMousePositionOnBoard(e);
			var node = GetHotNode();

			if (e.ChangedButton == MouseButton.Left)
			{
				if (node == null)
				{
					if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
						_selectedNodes.Clear();

					_rectangleSelecting = true;
				}
				else
				{
					if (_selectedNodes.Count == 1 && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
						_selectedNodes.Clear();

					SelectNode(node);
				}
			}
		}

		public void OnMouseWheel(MouseWheelEventArgs e)
		{
			var factor = e.Delta > 0 ? 2 : 0.5f;

			var (x, y) = GetMousePositionOnBoard(e);
			_boardTransform = _boardTransform.PreConcat(SKMatrix.CreateScale(factor, factor, x, y));
		}

		public void OnSizeChanged(SizeChangedEventArgs e)
		{
		}

		public void OnRender(TimeSpan dt)
		{
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

			_canvas.Clear(SKColors.White);

			var gridPaint = new SKPaint
			{
				Color = new SKColor(0xFF_EFEFEF),
				StrokeWidth = 1,
				IsAntialias = true
			};

			var selectionBoxPaint = new SKPaint
			{
				Color = new SKColor(0x47_037AFF),
				StrokeWidth = 1,
				IsAntialias = true
			};

			// render the canvas
			using (new SKAutoCanvasRestore(_canvas, true))
			{
				const int localGridPitch = 50;
				var originOffset = _boardTransform.MapPoint(0, 0);
				var gridPitch = Math.Max((int) (localGridPitch * _boardTransform.MapRadius(1)), 1);

				DrawViewportGrid(gridPitch, originOffset, gridPaint);
				_canvas.DrawCircle(originOffset, 3, gridPaint);

				_canvas.SetMatrix(_boardTransform);

				if (_rectangleSelecting)
					_canvas.DrawRect(_mouseDownPos.X, _mouseDownPos.Y, _lastMouseBoardPos.X - _mouseDownPos.X, _lastMouseBoardPos.Y - _mouseDownPos.Y, selectionBoxPaint);

				foreach (var node in _nodes) _nodeRenderer.DrawNode(_canvas, node, IsSelected(node));
			}

			_canvas.Flush();

			var err = GL.GetError();
			if (err != ErrorCode.NoError)
				Debug.WriteLine(err);

			GL.Finish();
		}

		private void DrawViewportGrid(int gridPitch, SKPoint boardOffset, SKPaint gridPaint)
		{
			for (var x = 0; x < _size.Width; x += gridPitch)
			{
				var lX = x + boardOffset.X % gridPitch;
				_canvas.DrawLine(lX, 0, lX, _size.Height, gridPaint);
			}

			for (var y = 0; y < _size.Height; y += gridPitch)
			{
				var lY = y + boardOffset.Y % gridPitch;
				_canvas.DrawLine(0, lY, _size.Width, lY, gridPaint);
			}
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