using System;
using Hyperwave.Extensions;
using Hyperwave.Helper;
using Hyperwave.Model;
using Hyperwave.Render;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SimpleUndoRedo;
using SkiaSharp;

namespace Hyperwave.Controls
{
	public class TextBoxControl : Control, ITransformableContents
	{
		private class DeleteTextCommand : ICommand<TextBoxControl>
		{
			private readonly int _endIndex;
			private readonly string _removedText;
			private readonly int _startIndex;

			public DeleteTextCommand(TextBoxControl control, int startIndex, int endIndex)
			{
				_startIndex = startIndex;
				_endIndex = endIndex;

				_removedText = control.Text[startIndex..endIndex];
			}

			public void Apply(TextBoxControl source)
			{
				source.Text = source.Text[.._startIndex] + source.Text[_endIndex..];
				source.Selection = new Selection(_startIndex);
			}

			public void Revert(TextBoxControl source)
			{
				source.Text = source.Text[.._startIndex] + _removedText + source.Text[_startIndex..];
				source.Selection = new Selection(_startIndex, _startIndex + _removedText.Length);
			}
		}

		private class InsertTextCommand : ICommand<TextBoxControl>
		{
			private readonly int _endIndex;
			private readonly string _replacedText;
			private readonly string _s;
			private readonly int _startIndex;

			public InsertTextCommand(TextBoxControl control, int startIndex, int endIndex, string s)
			{
				_startIndex = startIndex;
				_endIndex = endIndex;
				_s = s;
				_replacedText = control.Text[startIndex..endIndex];
			}

			public void Apply(TextBoxControl source)
			{
				source.Text = source.Text[.._startIndex] + _s + source.Text[_endIndex..];
				source.Selection = new Selection(_startIndex + _s.Length);
			}

			public void Revert(TextBoxControl source)
			{
				source.Text = source.Text[.._startIndex] + _replacedText + source.Text[(_startIndex + _s.Length)..];
				source.Selection = new Selection(_startIndex);
			}
		}

		public string[] Lines;

		private readonly CommandStack<TextBoxControl> _commandStack;
		private Selection _selection;
		private string _text;

		public SKMatrix ContentTransformation { get; set; } = SKMatrix.Identity;
		public ITextBoxRenderer Renderer { get; set; }

		public Selection Selection
		{
			get => _selection;
			set
			{
				var start = MathHelper.Clamp(value.StartIndex, 0, Text.Length);
				var end = MathHelper.Clamp(value.EndIndex, 0, Text.Length);

				var cursorSide = value.CursorSide;
				if (start > end)
				{
					var temp = start;
					start = end;
					end = temp;

					cursorSide = cursorSide switch
					{
						CursorSide.End => CursorSide.Start,
						CursorSide.Start => CursorSide.End,
						_ => throw new ArgumentOutOfRangeException()
					};
				}

				_selection = new Selection(start, end, cursorSide);

				CursorManager.ResetBlinking();
			}
		}

		public string Text
		{
			get => _text;
			set
			{
				_text = value;
				Lines = _text.Split('\n');
			}
		}

		public TextBoxControl()
		{
			Text = string.Empty;
			_commandStack = new CommandStack<TextBoxControl>(this);
			_selection = new Selection(0);
		}

		public override void ConsumeKeyDown(KeyboardKeyEventArgs args)
		{
			CursorManager.ResetBlinking();

			switch (args.Key)
			{
				case Keys.Z:
				{
					if (args.Control)
						_commandStack.Undo();

					break;
				}
				case Keys.Y:
				{
					if (args.Control)
						_commandStack.Redo();

					break;
				}
				case Keys.Home:
				{
					if (args.Shift)
					{
						if (Selection.CursorSide == CursorSide.Start)
							Selection = new Selection(0, Selection.EndIndex, CursorSide.Start);
						else
							Selection = new Selection(0, Selection.StartIndex, CursorSide.Start);
					}
					else
					{
						Selection = new Selection(0);
					}

					break;
				}
				case Keys.End:
				{
					if (args.Shift)
					{
						if (Selection.CursorSide == CursorSide.End)
							Selection = new Selection(Selection.StartIndex, Text.Length);
						else
							Selection = new Selection(Selection.EndIndex, Text.Length);
					}
					else
					{
						Selection = new Selection(Text.Length);
					}

					break;
				}
				case Keys.Tab:
				{
					// TODO: better tab handling
					InsertTextAtCursor("    ");
					break;
				}
				case Keys.Enter:
				{
					InsertTextAtCursor("\n");
					break;
				}
				case Keys.Backspace:
				{
					if (Selection.Length > 0)
					{
						_commandStack.ApplyCommand(
							new DeleteTextCommand(this, Selection.StartIndex, Selection.EndIndex));
					}
					else if (Selection.StartIndex > 0)
					{
						if (args.Control)
						{
							int lastIndex;

							if (ShouldControlThrough(Text[Selection.StartIndex - 1]))
								lastIndex = Text[..Selection.StartIndex].LastIndexOf(c => !ShouldControlThrough(c));
							else
								lastIndex = Text[..Selection.StartIndex].LastIndexOf(ShouldControlThrough);

							lastIndex++;

							_commandStack.ApplyCommand(new DeleteTextCommand(this, lastIndex, Selection.StartIndex));
						}
						else
						{
							_commandStack.ApplyCommand(new DeleteTextCommand(this, Selection.StartIndex - 1,
								Selection.StartIndex));
						}
					}

					break;
				}
				case Keys.Left:
				{
					var proposedSelection = Selection.MoveCursor(CursorDirection.Left, 1);
					if (args.Control)
						proposedSelection = GetCursorPositionAfterWordJump(CursorDirection.Left);

					if (args.Shift)
					{
						if (Selection.Length > 0)
							Selection = proposedSelection;
						else
							// Expand cursor into selection
							Selection = new Selection(proposedSelection.StartIndex, Selection.EndIndex,
								CursorSide.Start);
					}
					else if (Selection.Length > 0)
					{
						Selection = args.Control ? proposedSelection : new Selection(Selection.StartIndex);
					}
					else
					{
						Selection = proposedSelection;
					}

					break;
				}
				case Keys.Right:
				{
					var proposedSelection = Selection.MoveCursor(CursorDirection.Right, 1);
					if (args.Control)
						proposedSelection = GetCursorPositionAfterWordJump(CursorDirection.Right);

					if (args.Shift)
					{
						if (Selection.Length > 0)
							Selection = proposedSelection;
						else
							// Expand cursor into selection
							Selection = new Selection(Selection.StartIndex, proposedSelection.EndIndex);
					}
					else if (Selection.Length > 0)
					{
						Selection = args.Control ? proposedSelection : new Selection(Selection.EndIndex);
					}
					else
					{
						Selection = proposedSelection;
					}

					break;
				}
			}
		}

		public override void ConsumeMouseDown(MouseButtonEventArgs args, Vector2 mousePosition)
		{
			var localPosition = Transformation.Invert().MapPoint(mousePosition);
			var (_, _, cursorIndex) = Renderer.GetCursorAtPosition(this, localPosition);
			Selection = new Selection(cursorIndex);
		}

		public override void ConsumeTextInput(TextInputEventArgs args)
		{
			InsertTextAtCursor(args.AsString);
		}

		public override void Draw(SKCanvas canvas)
		{
			Renderer.Draw(this, canvas);
		}

		public (int lineIndex, int lineOffset) GetCursorLine(int cursorIndex)
		{
			for (var i = 0; i < Lines.Length; i++)
			{
				var lineLength = Lines[i].Length;

				if (cursorIndex <= lineLength)
					return (i, cursorIndex);

				cursorIndex -= lineLength + 1;
			}

			return (-1, -1);
		}

		public void InsertTextAtCursor(string s)
		{
			_commandStack.ApplyCommand(new InsertTextCommand(this, Selection.StartIndex, Selection.EndIndex,
				s));
			CursorManager.ResetBlinking();
		}

		private Selection GetCursorPositionAfterWordJump(CursorDirection direction)
		{
			var cursorIndex = Selection.CursorSide == CursorSide.End ? Selection.EndIndex : Selection.StartIndex;
			var noSelection = Selection.Length == 0;

			switch (direction)
			{
				case CursorDirection.Left:
				{
					if (cursorIndex == 0)
						return Selection;

					var reverseIndex = cursorIndex - 1;

					if (ShouldControlThrough(Text[cursorIndex - 1]))
						reverseIndex = Text[..cursorIndex].LastIndexOf(c => !ShouldControlThrough(c));
					else
						reverseIndex = Text[..cursorIndex].LastIndexOf(ShouldControlThrough);

					reverseIndex++;

					if (noSelection)
						return new Selection(reverseIndex);

					return Selection.MoveCursor(CursorDirection.Left, cursorIndex - reverseIndex);
				}
				case CursorDirection.Right:
				{
					if (cursorIndex >= Text.Length)
						return Selection;

					var forwardDistance = 1;

					if (ShouldControlThrough(Text[cursorIndex]))
						forwardDistance = Text[cursorIndex..].IndexOf(c => !ShouldControlThrough(c));
					else
						forwardDistance = Text[cursorIndex..].IndexOf(ShouldControlThrough);

					if (forwardDistance == -1)
						forwardDistance = Text[cursorIndex..].Length;

					return Selection.MoveCursor(direction, forwardDistance);
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
			}
		}

		private static bool ShouldControlThrough(char c)
		{
			return char.IsWhiteSpace(c) || char.IsPunctuation(c);
		}
	}
}