using System;
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
				var start = MathHelper.Clamp(Math.Min(value.StartIndex, value.EndIndex), 0, Text.Length);
				var end = MathHelper.Clamp(Math.Max(value.StartIndex, value.EndIndex), 0, Text.Length);

				_selection = new Selection(start, end, value.CursorSide);
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
					if (args.Shift)
					{
						if (Selection.Length > 0)
							Selection = Selection.CursorSide switch
							{
								CursorSide.Start => new Selection(Selection.StartIndex - 1, Selection.EndIndex,
									CursorSide.Start),
								CursorSide.End => new Selection(Selection.StartIndex, Selection.EndIndex - 1),
								_ => Selection
							};
						else
							Selection = new Selection(Selection.StartIndex - 1, Selection.EndIndex, CursorSide.Start);
					}
					else if (Selection.Length > 0)
					{
						Selection = new Selection(Selection.StartIndex);
					}
					else
					{
						Selection = new Selection(Selection.StartIndex - 1);
					}

					break;
				}
				case Keys.Right:
				{
					if (args.Shift)
					{
						if (Selection.Length > 0)
							Selection = Selection.CursorSide switch
							{
								CursorSide.Start => new Selection(Selection.StartIndex + 1, Selection.EndIndex,
									CursorSide.Start),
								CursorSide.End => new Selection(Selection.StartIndex, Selection.EndIndex + 1),
								_ => Selection
							};
						else
							Selection = new Selection(Selection.StartIndex, Selection.EndIndex + 1);
					}
					else if (Selection.Length > 0)
					{
						Selection = new Selection(Selection.EndIndex);
					}
					else
					{
						Selection = new Selection(Selection.EndIndex + 1);
					}

					break;
				}
			}
		}

		public override void ConsumeTextInput(TextInputEventArgs args)
		{
			_commandStack.ApplyCommand(new InsertTextCommand(this, Selection.StartIndex, Selection.EndIndex,
				args.AsString));
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

			_control = new TextBoxControl();
			FocusManager.FocusedControl = _control;
		}

		/// <inheritdoc />
		protected override void Draw(SKCanvas canvas)
		{
			using var textPaint = new SKPaint(new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 36))
			{
				Color = 0xFF_000000,
				IsStroke = false,
				IsAntialias = true,
				HintingLevel = SKPaintHinting.Full,
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

			var selectionStartX = textPaint.MeasureText(_control.Text[.._control.Selection.StartIndex]);
			var selectionEndX = textPaint.MeasureText(_control.Text[.._control.Selection.EndIndex]);
			canvas.DrawText(_control.Text, 50, 50, textPaint);

			var lineHeight = (int)(textPaint.FontMetrics.Descent - textPaint.FontMetrics.Ascent +
			                       textPaint.FontMetrics.Leading);
			var cursorPos = _control.Selection.CursorSide == CursorSide.Start ? selectionStartX : selectionEndX;
			canvas.DrawRect(50 + cursorPos, 50 + textPaint.FontMetrics.Ascent, 2, lineHeight, cursorPaint);

			if (selectionStartX != selectionEndX)
				canvas.DrawRect(50 + selectionStartX, 50 + textPaint.FontMetrics.Ascent,
					selectionEndX - selectionStartX, lineHeight, highlightPaint);
		}
	}
}