using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Comlink.Model;
using Comlink.Model.Nodes;
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

		private readonly List<Node> _nodes = new()
		{
			new InteractNode
			{
				X = 70,
				Y = 70
			},
			new BranchNode
			{
				X = 300,
				Y = 150
			},
			new ExitNode
			{
				X = 400,
				Y = 250
			}
		};

		private readonly List<Node> _selectedNodes = new();
		private readonly List<Node> _selectedNodesQueue = new();

		private SKMatrix _boardTransform = SKMatrix.Identity;
		private SKCanvas _canvas;
		private PinIdentifier _dragSourcePin;
		private GRGlFramebufferInfo _glInfo;
		private GRContext _grContext;
		private Vector2 _lastMouseBoardPos = Vector2.Zero;
		private Vector2 _lastMouseControlPos = Vector2.Zero;
		private SKSizeI _lastSize;
		private Vector2 _mouseDownPos = Vector2.Zero;

		private bool _rectangleSelecting;
		private GRBackendRenderTarget _renderTarget;

		private SKSizeI _size;
		private SKSurface _surface;

		private static bool IsDeleteConnectionKeyDown => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

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

		public bool HasSource(IPin pin)
		{
			return _nodes.SelectMany(node => node.Connections).Any(connection => connection.DestPinId == pin.PinId);
		}

		public bool HasDestination(IPin pin)
		{
			return _nodes.SelectMany(node => node.Connections).Any(connection => connection.SourcePinId == pin.PinId);
		}

		public void RemoveAllConnections(IPin pin)
		{
			foreach (var node in _nodes)
				node.Connections.RemoveAll(connection => connection.SourcePinId == pin.PinId || connection.DestPinId == pin.PinId);
		}

		private void CreateConnection(PinIdentifier a, PinIdentifier b)
		{
			if (!a.Pin.CanConnectTo(b.Pin))
				return;

			if (b.Pin is IOutputPin && a.Pin is IInputPin)
			{
				// b -> a

				if (a.Pin is TypeInputPin && HasSource(a.Pin))
					return;

				if (b.Pin is FlowOutputPin && HasDestination(b.Pin))
					return;

				b.Node.Connections.Add(new Connection(b.Node.NodeId, b.Pin.PinId, a.Node.NodeId, a.Pin.PinId));
			}
			else if (a.Pin is IOutputPin && b.Pin is IInputPin)
			{
				// a -> b

				if (b.Pin is TypeInputPin && HasSource(b.Pin))
					return;

				if (a.Pin is FlowOutputPin && HasDestination(a.Pin))
					return;

				a.Node.Connections.Add(new Connection(a.Node.NodeId, a.Pin.PinId, b.Node.NodeId, b.Pin.PinId));
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		private PinIdentifier GetPin(Guid nodeId, Guid pinId)
		{
			var node = _nodes.FirstOrDefault(node1 => node1.NodeId == nodeId);
			if (node == null)
				return null;

			var pin = (IPin) node.InputPins.FirstOrDefault(inputPin => inputPin.PinId == pinId) ?? node.OutputPins.FirstOrDefault(outputPin => outputPin.PinId == pinId);

			return pin == null ? null : new PinIdentifier(node, pin);
		}

		public void OnMouseUp(MouseButtonEventArgs e)
		{
			_rectangleSelecting = false;
			CommitSelectionQueue();

			if (_dragSourcePin != null)
			{
				var node = GetHotNode();

				if (node != null)
				{
					var pin = _nodeRenderer.GetPin(node, _lastMouseBoardPos.X, _lastMouseBoardPos.Y);

					if (pin != null && node.NodeId != _dragSourcePin.Node.NodeId)
						CreateConnection(_dragSourcePin, new PinIdentifier(node, pin));
				}
			}

			_dragSourcePin = null;
		}

		private void CommitSelectionQueue()
		{
			_selectedNodes.AddRange(_selectedNodesQueue);
			_selectedNodesQueue.Clear();
		}

		public void OnMouseDown(MouseButtonEventArgs e)
		{
			_mouseDownPos = _lastMouseBoardPos = GetMousePositionOnBoard(e);

			if (e.ChangedButton == MouseButton.Left)
			{
				var node = GetHotNode();
				IPin pin = null;

				if (node != null)
				{
					pin = _nodeRenderer.GetPin(node, _mouseDownPos.X, _mouseDownPos.Y);

					if (pin != null)
					{
						if (IsDeleteConnectionKeyDown)
							RemoveAllConnections(pin);
						else
							_dragSourcePin = new PinIdentifier(node, pin);
					}
				}

				if (node == null) // Selected empty space
				{
					if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
						_selectedNodes.Clear();

					_rectangleSelecting = true;
				}
				else if (pin == null) // Selected a node but not a pin
				{
					if (_selectedNodes.Count == 1 && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
						_selectedNodes.Clear();

					SelectNode(node);
				}
				else // Selected a pin
				{
					_selectedNodes.Clear();
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
				Style = SKPaintStyle.Stroke,
				StrokeWidth = 1,
				IsAntialias = true
			};

			var selectionBoxPaint = new SKPaint
			{
				Color = new SKColor(0x47_037AFF),
				Style = SKPaintStyle.Fill,
				StrokeWidth = 1,
				IsAntialias = true
			};

			var ephemeralConnectionPaint = new SKPaint
			{
				Color = new SKColor(0x47_808080),
				Style = SKPaintStyle.Stroke,
				StrokeWidth = 5,
				IsAntialias = true,
				StrokeCap = SKStrokeCap.Round
			};

			var connectionPaint = new SKPaint
			{
				Color = new SKColor(0x80_808080),
				Style = SKPaintStyle.Stroke,
				StrokeWidth = 5,
				IsAntialias = true,
				StrokeCap = SKStrokeCap.Round
			};

			var deleteConnectionPaint = new SKPaint
			{
				Color = new SKColor(0x80_FF0000),
				Style = SKPaintStyle.Stroke,
				StrokeWidth = 5,
				IsAntialias = true,
				StrokeCap = SKStrokeCap.Round
			};

			// render the canvas
			using (new SKAutoCanvasRestore(_canvas, true))
			{
				const int localGridPitch = 50;
				var originOffset = _boardTransform.MapPoint(0, 0);
				var gridPitch = (int) (localGridPitch * _boardTransform.MapRadius(1));

				if (gridPitch > 1)
					DrawViewportGrid(gridPitch, originOffset, gridPaint);

				_canvas.DrawCircle(originOffset, 3, gridPaint);

				_canvas.SetMatrix(_boardTransform);

				if (_rectangleSelecting)
					_canvas.DrawRect(_mouseDownPos.X, _mouseDownPos.Y, _lastMouseBoardPos.X - _mouseDownPos.X, _lastMouseBoardPos.Y - _mouseDownPos.Y, selectionBoxPaint);

				var hotNode = GetHotNode();
				IPin hotPin = null;

				if (hotNode != null)
					hotPin = _nodeRenderer.GetPin(hotNode, _lastMouseBoardPos.X, _lastMouseBoardPos.Y);

				DrawConnections(hotPin, ephemeralConnectionPaint, connectionPaint, deleteConnectionPaint);
				DrawNodes();
			}

			_canvas.Flush();

			var err = GL.GetError();
			if (err != ErrorCode.NoError)
				Debug.WriteLine(err);

			GL.Finish();
		}

		private void DrawNodes()
		{
			foreach (var node in _nodes) _nodeRenderer.DrawNode(_canvas, node, IsSelected(node));
		}

		private void DrawConnection(Vector2 start, Vector2 end, SKPaint paint)
		{
			var path = new SKPath();

			path.MoveTo(start.X, start.Y);

			var half = (start + end) / 2;

			path.QuadTo(start.X + 50, start.Y, half.X, half.Y);
			path.QuadTo(end.X - 50, end.Y, end.X, end.Y);

			_canvas.DrawPath(path, paint);
		}

		private void DrawConnections(IPin hotPin, SKPaint ephemeralConnectionPaint, SKPaint connectionPaint, SKPaint deleteConnectionPaint)
		{
			foreach (var node in _nodes)
			{
				foreach (var connection in node.Connections)
				{
					var outputPin = GetPin(connection.SourceNodeId, connection.SourcePinId);
					var inputPin = GetPin(connection.DestNodeId, connection.DestPinId);

					if (outputPin == null || inputPin == null)
						throw new InvalidOperationException();

					var outputPos = _nodeRenderer.GetPinPos(outputPin.Node, outputPin.Pin);
					var inputPos = _nodeRenderer.GetPinPos(inputPin.Node, inputPin.Pin);

					if (outputPos == null || inputPos == null)
						throw new InvalidOperationException();

					if (hotPin != null && (inputPin.Pin.PinId == hotPin.PinId || outputPin.Pin.PinId == hotPin.PinId) && IsDeleteConnectionKeyDown)
						DrawConnection(outputPos.Value, inputPos.Value, deleteConnectionPaint);
					else
						DrawConnection(outputPos.Value, inputPos.Value, connectionPaint);
				}
			}

			if (_dragSourcePin != null)
			{
				var posNullable = _nodeRenderer.GetPinPos(_dragSourcePin.Node, _dragSourcePin.Pin);
				if (!posNullable.HasValue)
					throw new InvalidOperationException();

				var node = GetHotNode();

				var paint = ephemeralConnectionPaint;

				var start = posNullable.Value;
				var end = _lastMouseBoardPos;

				if (node != null)
				{
					var pin = _nodeRenderer.GetPin(node, _lastMouseBoardPos.X, _lastMouseBoardPos.Y);

					if (pin != null && _dragSourcePin.Pin.CanConnectTo(pin) && node.NodeId != _dragSourcePin.Node.NodeId)
					{
						paint = connectionPaint;

						var pinPos = _nodeRenderer.GetPinPos(node, pin);
						if (!pinPos.HasValue)
							throw new InvalidOperationException();

						end = pinPos.Value;
					}
				}

				if (_dragSourcePin.Pin is IInputPin)
				{
					var temp = start;
					start = end;
					end = temp;
				}

				DrawConnection(start, end, paint);
			}
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
}