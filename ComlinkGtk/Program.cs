using System;
using GLib;
using Application = Gtk.Application;

namespace ComlinkGtk
{
	internal class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			Application.Init();

			var app = new Application("com.parzivail.ComlinkGtk", ApplicationFlags.None);
			app.Register(Cancellable.Current);

			var win = new MainWindow();
			app.AddWindow(win);

			win.Show();
			Application.Run();
		}
	}
}