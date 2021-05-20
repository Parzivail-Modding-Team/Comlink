using System;
using System.Drawing;
using ComlinkGtk.GraphicsBindings;
using Gtk;
using OpenTK.Graphics.OpenGL;
using UI = Gtk.Builder.ObjectAttribute;

namespace ComlinkGtk
{
	internal class MainWindow : Window
	{
		[UI] private GLArea _viewport;

		public MainWindow() : this(new Builder("comlink.glade"))
		{
		}

		private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
		{
			builder.Autoconnect(this);

			DeleteEvent += Window_DeleteEvent;

			_viewport.Realized += ViewportCreated;
			_viewport.Render += ViewportOnRender;
		}

		private void ViewportCreated(object sender, EventArgs e)
		{
			_viewport.MakeCurrent();

			GL.LoadBindings(new NativeBindingsContext());
		}

		private void ViewportOnRender(object sender, RenderArgs e)
		{
			_viewport.MakeCurrent();

			GL.ClearColor(Color.Chartreuse);
			GL.Clear(ClearBufferMask.ColorBufferBit);
		}

		private void Window_DeleteEvent(object sender, DeleteEventArgs a)
		{
			Application.Quit();
		}
	}
}