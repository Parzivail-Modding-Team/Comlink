using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Comlink.Command;
using Comlink.Model;
using Nedry.Pin;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using SkiaSharp;

namespace Comlink.Render
{
	public class GraphRenderer : IDisposable
	{
		private const SKColorType ColorType = SKColorType.Rgba8888;
		private const GRSurfaceOrigin SurfaceOrigin = GRSurfaceOrigin.BottomLeft;

		private static SKPaint _baseNodePaint;
		private static SKPaint _gridPaint;
		private static SKPaint _originPaint;
		private static SKPaint _selectionBoxPaint;
		private static SKPaint _ephemeralConnectionPaint;
		private static SKPaint _connectionPaint;
		private static SKPaint _deleteConnectionPaint;

		private readonly GLWpfControl _control;
		private readonly NodeRenderer _nodeRenderer;

		private readonly List<ComlinkNode> _selectedNodesQueue = new();

		private SKMatrix _boardTransform = SKMatrix.Identity;
		private SKCanvas _canvas;
		private PinReference _dragSourcePin;
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

		public event EventHandler<ICommand<Graph>> CommandExecuted;
		public event EventHandler<EventArgs> SelectionChanged;

		private static bool IsDeleteConnectionKeyDown => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

		public Graph TargetGraph { get; set; }

		public bool HasSelection => Selection.Count > 0;
		public List<ComlinkNode> Selection { get; } = new();

		public GraphRenderer(Graph graph, GLWpfControl control)
		{
			TargetGraph = graph;
			_control = control;

			SetupStyles();

			_nodeRenderer = new NodeRenderer(
				_baseNodePaint,
				SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.Medium, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
				SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
			);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_canvas.Dispose();
			_grContext.Dispose();
			_renderTarget.Dispose();
			_surface.Dispose();
		}

		private static void SetupStyles()
		{
			_baseNodePaint = new SKPaint
			{
				IsAntialias = true,
				TextSize = 16,
				SubpixelText = true,
				LcdRenderText = true,
				IsAutohinted = true
			};

			_gridPaint = new SKPaint
			{
				Color = new SKColor(0xFF_EFEFEF),
				Style = SKPaintStyle.Stroke,
				StrokeWidth = 1,
				IsAntialias = true
			};

			_originPaint = new SKPaint
			{
				Color = new SKColor(0xFF_D0D0D0),
				Style = SKPaintStyle.Fill,
				IsAntialias = true
			};

			_selectionBoxPaint = new SKPaint
			{
				Color = new SKColor(0x47_037AFF),
				Style = SKPaintStyle.Fill,
				StrokeWidth = 1,
				IsAntialias = true
			};

			_ephemeralConnectionPaint = new SKPaint
			{
				Color = new SKColor(0x47_808080),
				Style = SKPaintStyle.Stroke,
				StrokeWidth = 5,
				IsAntialias = true,
				StrokeCap = SKStrokeCap.Round
			};

			_connectionPaint = new SKPaint
			{
				Color = new SKColor(0x80_808080),
				Style = SKPaintStyle.Stroke,
				StrokeWidth = 5,
				IsAntialias = true,
				StrokeCap = SKStrokeCap.Round
			};

			_deleteConnectionPaint = new SKPaint
			{
				Color = new SKColor(0x80_FF0000),
				Style = SKPaintStyle.Stroke,
				StrokeWidth = 5,
				IsAntialias = true,
				StrokeCap = SKStrokeCap.Round
			};
		}

		private Vector2 GetMousePositionOnBoard(MouseEventArgs e)
		{
			var pos = e.GetPosition(_control);
			return ControlToBoardCoords((float) pos.X, (float) pos.Y);
		}

		public Vector2 ControlToBoardCoords(float x, float y)
		{
			var transformedPoint = _boardTransform.Invert().MapPoint(x, y);
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
						foreach (var selectedNode in Selection)
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

		private ComlinkNode GetHotNode()
		{
			return TargetGraph.LastOrDefault(node => _nodeRenderer.GetBounds(node).Contains(new Vector2(_lastMouseBoardPos.X, _lastMouseBoardPos.Y)));
		}

		private bool IsSelected(ComlinkNode node)
		{
			return Selection.Contains(node) || _selectedNodesQueue.Contains(node);
		}

		private void SelectNode(ComlinkNode node)
		{
			if (IsSelected(node))
				return;

			Selection.Add(node);
		}

		private void SelectAllInSelectionRectangle()
		{
			var selectionRect = new Box2(_mouseDownPos.X, _mouseDownPos.Y, _lastMouseBoardPos.X, _lastMouseBoardPos.Y);

			_selectedNodesQueue.Clear();
			foreach (var node in TargetGraph.Where(node => !Selection.Contains(node)))
				if (_nodeRenderer.GetBounds(node).Contains(selectionRect))
					_selectedNodesQueue.Add(node);
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
						OnCommandExecuted(new CreateConnectionCommand(_dragSourcePin.Pin.PinId, pin.PinId));
				}
			}

			_dragSourcePin = null;
		}

		private void CommitSelectionQueue()
		{
			Selection.AddRange(_selectedNodesQueue);
			_selectedNodesQueue.Clear();

			OnSelectionChanged();
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
						{
							var connections = TargetGraph
								.SelectMany(n => n
									.Connections
									.Where(connection => connection.Source == pin.PinId || connection.Destination == pin.PinId)
								).ToArray();
							if (connections.Length > 0)
								OnCommandExecuted(new DeleteConnectionsCommand(connections));
						}
						else
						{
							_dragSourcePin = new PinReference(node, pin);
						}
					}
				}

				if (node == null) // Selected empty space
				{
					if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
						SelectNone();

					_rectangleSelecting = true;
				}
				else if (pin == null) // Selected a node but not a pin
				{
					if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift) && !Selection.Contains(node))
						SelectNone();

					SelectNode(node);
				}
				else // Selected a pin
				{
					SelectNone();
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

			// render the canvas
			using (new SKAutoCanvasRestore(_canvas, true))
			{
				const int localGridPitch = 50;
				var originOffset = _boardTransform.MapPoint(0, 0);
				var gridPitch = (int) (localGridPitch * _boardTransform.MapRadius(1));

				if (gridPitch > 1)
					DrawViewportGrid(gridPitch, originOffset, _gridPaint);

				_canvas.DrawCircle(originOffset, 3, _originPaint);

				_canvas.SetMatrix(_boardTransform);

				if (_rectangleSelecting)
					_canvas.DrawRect(_mouseDownPos.X, _mouseDownPos.Y, _lastMouseBoardPos.X - _mouseDownPos.X, _lastMouseBoardPos.Y - _mouseDownPos.Y, _selectionBoxPaint);

				var hotNode = GetHotNode();
				IPin hotPin = null;

				if (hotNode != null)
					hotPin = _nodeRenderer.GetPin(hotNode, _lastMouseBoardPos.X, _lastMouseBoardPos.Y);

				DrawConnections(hotPin, _ephemeralConnectionPaint, _connectionPaint, _deleteConnectionPaint);
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
			foreach (var node in TargetGraph) _nodeRenderer.DrawNode(_canvas, node, IsSelected(node));
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
			foreach (var node in TargetGraph)
			{
				foreach (var connection in node.Connections)
				{
					var outputPin = TargetGraph.GetPin(connection.Source);
					var inputPin = TargetGraph.GetPin(connection.Destination);

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

		public void SelectAll()
		{
			_selectedNodesQueue.AddRange(TargetGraph);
			CommitSelectionQueue();
		}

		public void SelectNone()
		{
			Selection.Clear();

			OnSelectionChanged();
		}

		public void SelectInverse()
		{
			_selectedNodesQueue.AddRange(TargetGraph.Where(node => !Selection.Contains(node)));
			SelectNone();
			CommitSelectionQueue();
		}

		protected virtual void OnCommandExecuted(ICommand<Graph> e)
		{
			CommandExecuted?.Invoke(this, e);
		}

		protected virtual void OnSelectionChanged()
		{
			SelectionChanged?.Invoke(this, EventArgs.Empty);
		}

		public Vector2 GetViewportCenter()
		{
			return ControlToBoardCoords((float) (_control.ActualWidth / 2), (float) (_control.ActualHeight / 2));
		}
	}
}