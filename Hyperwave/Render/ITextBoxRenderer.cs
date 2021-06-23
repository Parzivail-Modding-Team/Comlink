using Hyperwave.Controls;
using Hyperwave.Model;
using OpenTK.Mathematics;

namespace Hyperwave.Render
{
	public interface ITextBoxRenderer : IRenderer<TextBoxControl>
	{
		public (int lineIndex, int lineOffset, int cursorIndex) GetCursorAtPosition(TextBoxControl control, Vector2 pos,
			LineOffset previousLine = LineOffset.None);

		public Vector2 GetCursorPosition(TextBoxControl control);
	}
}