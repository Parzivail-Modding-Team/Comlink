using Hyperwave.Controls;
using OpenTK.Mathematics;

namespace Hyperwave.Render
{
	public interface ITextBoxRenderer : IRenderer<TextBoxControl>
	{
		public (int lineIndex, int lineOffset, int cursorIndex)
			GetCursorAtPosition(TextBoxControl control, Vector2 pos);
	}
}