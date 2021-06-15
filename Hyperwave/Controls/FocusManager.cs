using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace Hyperwave.Controls
{
	internal static class FocusManager
	{
		public static Control FocusedControl { get; set; }

		public static void KeyDown(KeyboardKeyEventArgs args)
		{
			FocusedControl?.ConsumeKeyDown(args);
		}

		public static void MouseDown(MouseButtonEventArgs args, Vector2 mousePosition)
		{
			FocusedControl?.ConsumeMouseDown(args, mousePosition);
		}

		public static void TextInput(TextInputEventArgs args)
		{
			FocusedControl?.ConsumeTextInput(args);
		}
	}
}