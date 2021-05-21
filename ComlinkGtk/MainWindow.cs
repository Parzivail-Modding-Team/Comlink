using System;
using System.Collections.Generic;
using Comlink.Command;
using Comlink.Project;
using Comlink.Render;
using ComlinkGtk.GraphicsBindings;
using Gdk;
using Gtk;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using UI = Gtk.Builder.ObjectAttribute;
using Window = Gtk.Window;

namespace ComlinkGtk
{
	internal class MainWindow : Window, IViewport
	{
		private readonly GraphRenderer _graphRenderer;
		private ComlinkProject _loadedProject;

		private readonly Dictionary<uint, MouseButton> _mouseButtons = new()
		{
			{1, MouseButton.Left},
			{3, MouseButton.Right}
		};

		private readonly Dictionary<MouseButton, bool> _mouseState = new();

		[UI] private GLArea _viewport;
		[UI] private MenuItem _fileOpen;

		public ComlinkProject LoadedProject
		{
			get => _loadedProject;
			set
			{
				_loadedProject = value;
				if (_graphRenderer != null)
					_graphRenderer.TargetGraph = _loadedProject.Graph;
			}
		}

		/// <inheritdoc />
		public int Framebuffer { get; private set; }

		/// <inheritdoc />
		public int Width { get; private set; }

		/// <inheritdoc />
		public int Height { get; private set; }

		public MainWindow() : this(new Builder("comlink.glade"))
		{
		}

		private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
		{
			builder.Autoconnect(this);

			foreach (var value in _mouseButtons.Values)
				_mouseState[value] = false;

			DeleteEvent += Window_DeleteEvent;

			WireMenu();
			WireViewport();

			LoadedProject = ComlinkProject.NewEmptyProject();
			_graphRenderer = new GraphRenderer(LoadedProject.Graph, this);
			_graphRenderer.CommandExecuted += GraphRendererOnCommandExecuted;
			_graphRenderer.SelectionChanged += SelectedNodesChanged;
		}

		/// <inheritdoc />
		public bool IsKeyDown(Keys key)
		{
			return false;
		}

		private void WireMenu()
		{
			_fileOpen.Activated += FileOpenOnActivated;
		}

		private void WireViewport()
		{
			_viewport.AddEvents((int) (
				EventMask.ButtonPressMask |
				EventMask.ButtonReleaseMask |
				EventMask.PointerMotionMask |
				EventMask.ScrollMask |
				EventMask.KeyPressMask |
				EventMask.KeyReleaseMask
			));

			_viewport.ButtonPressEvent += ViewportOnButtonPressEvent;
			_viewport.ButtonReleaseEvent += ViewportOnButtonReleaseEvent;
			_viewport.MotionNotifyEvent += ViewportOnMotionNotifyEvent;
			_viewport.Realized += ViewportCreated;
			_viewport.Render += ViewportOnRender;
			_viewport.Resize += (o, args) =>
			{
				Width = args.Width;
				Height = args.Height;
			};
			_viewport.ScrollEvent += ViewportOnScrollEvent;
		}

		private void FileOpenOnActivated(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		private void ViewportOnScrollEvent(object o, ScrollEventArgs args)
		{
			_graphRenderer.OnMouseWheel(args.Event.Direction == ScrollDirection.Up ? 1 : -1, new Vector2((float) args.Event.X, (float) args.Event.Y));
		}

		private void ViewportOnMotionNotifyEvent(object o, MotionNotifyEventArgs args)
		{
			_graphRenderer.OnMouseMove(new Vector2((float) args.Event.X, (float) args.Event.Y), _mouseState[MouseButton.Left], _mouseState[MouseButton.Right]);
		}

		private void ViewportOnButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
		{
			if (!_mouseButtons.ContainsKey(args.Event.Button))
				return;

			var button = _mouseButtons[args.Event.Button];
			_mouseState[button] = false;

			_graphRenderer.OnMouseUp(button, new Vector2((float) args.Event.X, (float) args.Event.Y));
		}

		private void ViewportOnButtonPressEvent(object o, ButtonPressEventArgs args)
		{
			if (!_mouseButtons.ContainsKey(args.Event.Button))
				return;

			var button = _mouseButtons[args.Event.Button];
			_mouseState[button] = true;

			_graphRenderer.OnMouseDown(button, new Vector2((float) args.Event.X, (float) args.Event.Y));
		}

		private void GraphRendererOnCommandExecuted(object sender, ICommand<Graph> e)
		{
			_loadedProject.CommandStack.ApplyCommand(e);
		}

		private void SelectedNodesChanged(object sender, EventArgs eventArgs)
		{
			// TODO
		}

		private void ViewportCreated(object sender, EventArgs e)
		{
			_viewport.MakeCurrent();

			GL.LoadBindings(new NativeBindingsContext());
		}

		private void ViewportOnRender(object sender, RenderArgs e)
		{
			_viewport.MakeCurrent();
			Framebuffer = GL.GetInteger(GetPName.FramebufferBinding);

			_graphRenderer.OnRender();

			_viewport.QueueDraw();
		}

		private void Window_DeleteEvent(object sender, DeleteEventArgs a)
		{
			Application.Quit();
		}
	}
}