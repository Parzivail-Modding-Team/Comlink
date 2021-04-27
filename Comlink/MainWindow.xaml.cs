using System;
using System.Windows;
using System.Windows.Input;
using Comlink.Render;
using OpenTK.Wpf;

namespace Comlink
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly GraphRenderer _graphRenderer;

		public MainWindow()
		{
			InitializeComponent();

			var settings = new GLWpfControlSettings
			{
				MajorVersion = 3,
				MinorVersion = 1,
				RenderContinuously = true
			};
			Viewport.Start(settings);

			_graphRenderer = new GraphRenderer(Viewport);
		}

		private void Viewport_OnMouseMove(object sender, MouseEventArgs e)
		{
			_graphRenderer.OnMouseMove(e);
		}

		private void Viewport_OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			_graphRenderer.OnMouseDown(e);
		}

		private void Viewport_OnMouseWheel(object sender, MouseWheelEventArgs e)
		{
			_graphRenderer.OnMouseWheel(e);
		}

		private void Viewport_OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			_graphRenderer.OnSizeChanged(e);
		}

		private void Viewport_OnRender(TimeSpan obj)
		{
			_graphRenderer.OnRender(obj);
		}
	}
}