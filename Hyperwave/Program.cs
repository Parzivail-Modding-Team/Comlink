using System;
using System.Linq;
using Hyperwave.Extensions;
using Hyperwave.Model;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SimpleUndoRedo;
using SkiaSharp;

namespace Hyperwave
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			GLFW.WindowHint(WindowHintInt.StencilBits, 8);
			new NodeRenderingWindow().Run();
		}
	}

	internal class FocusManager
	{
		public static Control FocusedControl { get; set; }

		public static void KeyDown(KeyboardKeyEventArgs args)
		{
			FocusedControl?.ConsumeKeyDown(args);
		}

		public static void TextInput(TextInputEventArgs args)
		{
			FocusedControl?.ConsumeTextInput(args);
		}
	}

	internal class Control
	{
		public int RequestedX { get; set; }
		public int RequestedY { get; set; }

		public virtual void ConsumeKeyDown(KeyboardKeyEventArgs args)
		{
		}

		public virtual void ConsumeTextInput(TextInputEventArgs args)
		{
		}
	}

	internal class TextBoxControl : Control
	{
		private readonly CommandStack<TextBoxControl> _commandStack;
		private Selection _selection;

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
			}
		}

		public string Text { get; set; } = "";

		public TextBoxControl()
		{
			_commandStack = new CommandStack<TextBoxControl>(this);
			_selection = new Selection(0);
		}

		public override void ConsumeKeyDown(KeyboardKeyEventArgs args)
		{
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

		public override void ConsumeTextInput(TextInputEventArgs args)
		{
			InsertTextAtCursor(args.AsString);
		}

		public void InsertTextAtCursor(string s)
		{
			_commandStack.ApplyCommand(new InsertTextCommand(this, Selection.StartIndex, Selection.EndIndex,
				s));
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
	}

	public class NodeRenderingWindow : SkiaWindow
	{
		private readonly TextBoxControl _control;

		/// <inheritdoc />
		public NodeRenderingWindow() : base(new GameWindowSettings { RenderFrequency = 0 },
			new NativeWindowSettings { Size = new Vector2i(960, 540) })
		{
			KeyDown += FocusManager.KeyDown;
			TextInput += FocusManager.TextInput;

			_control = new TextBoxControl
			{
				RequestedX = 100,
				RequestedY = 100
			};
			FocusManager.FocusedControl = _control;
		}

		/// <inheritdoc />
		protected override void Draw(SKCanvas canvas)
		{
			using var textPaint = new SKPaint(new SKFont(SKTypeface.FromFamilyName("Georgia"), 18))
			{
				Color = 0xFF_000000,
				IsStroke = false,
				IsAntialias = true,
				SubpixelText = true,
				LcdRenderText = true
			};

			using var cursorPaint = new SKPaint
			{
				Color = 0xFF_000000,
				IsStroke = false
			};

			using var highlightPaint = new SKPaint
			{
				Color = 0x40_0000FF,
				IsStroke = false
			};

			var lineHeight = (int)(textPaint.FontMetrics.Descent - textPaint.FontMetrics.Ascent +
			                       textPaint.FontMetrics.Leading);

			var lines = _control.Text.Split("\n");

			var selectionStartLine = _control.Text[.._control.Selection.StartIndex].Count(c => c == '\n');
			var selectionEndLine = _control.Text[.._control.Selection.EndIndex].Count(c => c == '\n');

			var startIndexLineStart = _control.Text[.._control.Selection.StartIndex].LastIndexOf('\n') + 1;
			var endIndexLineStart = _control.Text[.._control.Selection.EndIndex].LastIndexOf('\n') + 1;

			var selectionStartOffset = _control.Selection.StartIndex - startIndexLineStart;
			var selectionEndOffset = _control.Selection.EndIndex - endIndexLineStart;

			var selectionStartX = textPaint.MeasureText(lines[selectionStartLine][..selectionStartOffset]);
			var selectionEndX = textPaint.MeasureText(lines[selectionEndLine][..selectionEndOffset]);

			var controlX = _control.RequestedX;
			var controlY = _control.RequestedY;

			for (var i = 0; i < lines.Length; i++)
			{
				var line = lines[i];
				canvas.DrawText(line, controlX, controlY + lineHeight * i, textPaint);

				if (_control.Selection.Length > 0)
				{
					float boxStartX = 0;
					float boxWidth = 0;

					if (i == selectionStartLine && selectionStartLine == selectionEndLine)
					{
						boxStartX = textPaint.MeasureText(line[..selectionStartOffset]);
						boxWidth = textPaint.MeasureText(line[selectionStartOffset..selectionEndOffset]);
					}
					else if (i == selectionStartLine)
					{
						boxStartX = textPaint.MeasureText(line[..selectionStartOffset]);
						boxWidth = textPaint.MeasureText(line[selectionStartOffset..]);
					}
					else if (i == selectionEndLine)
					{
						boxWidth = textPaint.MeasureText(line[..selectionEndOffset]);
					}
					else if (i > selectionStartLine && i < selectionEndLine)
					{
						boxWidth = textPaint.MeasureText(line);
					}

					canvas.DrawRect(controlX + boxStartX, controlY + textPaint.FontMetrics.Ascent + i * lineHeight,
						boxWidth, lineHeight, highlightPaint);
				}
			}

			var cursorX = _control.Selection.CursorSide == CursorSide.Start ? selectionStartX : selectionEndX;
			var cursorLine = _control.Selection.CursorSide == CursorSide.Start ? selectionStartLine : selectionEndLine;

			canvas.DrawRect(controlX + cursorX, controlY + textPaint.FontMetrics.Ascent + cursorLine * lineHeight, 1,
				lineHeight,
				cursorPaint);
		}
	}
}