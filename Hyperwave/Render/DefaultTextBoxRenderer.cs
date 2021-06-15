using System;
using System.Linq;
using Hyperwave.Controls;
using Hyperwave.Helper;
using Hyperwave.Model;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Hyperwave.Render
{
	public class DefaultTextBoxRenderer : ITextBoxRenderer
	{
		private readonly SKPaint _textPaint;

		public float LineHeight => _textPaint.FontMetrics.Descent - _textPaint.FontMetrics.Ascent +
		                           _textPaint.FontMetrics.Leading;

		public DefaultTextBoxRenderer()
		{
			_textPaint = new SKPaint(new SKFont(SKTypeface.FromFamilyName("Georgia"), 36))
			{
				Color = 0xFF_000000,
				IsStroke = false,
				IsAntialias = true,
				SubpixelText = true,
				LcdRenderText = true
			};
		}

		public void Draw(TextBoxControl control, SKCanvas canvas)
		{
			using var cursorPaint = new SKPaint
			{
				Color = 0xFF_000000,
				IsStroke = false,
				IsAntialias = true
			};

			using var highlightPaint = new SKPaint
			{
				Color = 0x60_0000FF,
				IsStroke = false
			};

			using var borderPaint = new SKPaint
			{
				Color = 0xFF_000000,
				IsStroke = true,
				StrokeWidth = 2
			};

			var lineHeight = (int)LineHeight;

			var lines = control.Lines;

			var (selectionStartLine, selectionStartOffset) = control.GetCursorLine(control.Selection.StartIndex);
			var (selectionEndLine, selectionEndOffset) = control.GetCursorLine(control.Selection.EndIndex);

			var selectionStartX = _textPaint.MeasureText(lines[selectionStartLine][..selectionStartOffset]);
			var selectionEndX = _textPaint.MeasureText(lines[selectionEndLine][..selectionEndOffset]);

			canvas.Save();
			canvas.SetMatrix(control.Transformation);

			// render control foreground

			// TODO: auto-width
			var controlWidth = control.Size.Width;
			var controlHeight = float.IsNaN(control.Size.Height) ? lineHeight * lines.Length : control.Size.Height;

			canvas.DrawRect(-2, -2, controlWidth + 4, controlHeight + 4, borderPaint);

			canvas.SetMatrix(canvas.TotalMatrix.PostConcat(control.ContentTransformation));
			canvas.Translate(0, -_textPaint.FontMetrics.Ascent);

			// render control contents
			for (var i = 0; i < lines.Length; i++)
			{
				var line = lines[i];

				if (control.Selection.Length > 0)
				{
					float boxStartX = 0;
					float boxWidth = 0;

					if (i == selectionStartLine && selectionStartLine == selectionEndLine)
					{
						boxStartX = _textPaint.MeasureText(line[..selectionStartOffset]);
						boxWidth = _textPaint.MeasureText(line[selectionStartOffset..selectionEndOffset]);
					}
					else if (i == selectionStartLine)
					{
						boxStartX = _textPaint.MeasureText(line[..selectionStartOffset]);
						boxWidth = _textPaint.MeasureText(line[selectionStartOffset..]);
					}
					else if (i == selectionEndLine)
					{
						boxWidth = _textPaint.MeasureText(line[..selectionEndOffset]);
					}
					else if (i > selectionStartLine && i < selectionEndLine)
					{
						boxWidth = _textPaint.MeasureText(line);
					}

					canvas.DrawRect(boxStartX, _textPaint.FontMetrics.Ascent + i * lineHeight,
						boxWidth, lineHeight, highlightPaint);
				}

				canvas.DrawText(line, 0, lineHeight * i, _textPaint);
			}

			if (CursorManager.CursorBlinkPhase)
			{
				var cursorX = control.Selection.CursorSide == CursorSide.Start ? selectionStartX : selectionEndX;
				var cursorLine = control.Selection.CursorSide == CursorSide.Start
					? selectionStartLine
					: selectionEndLine;

				canvas.DrawRect(MathF.Round(cursorX), _textPaint.FontMetrics.Ascent + cursorLine * lineHeight, 1,
					MathF.Round(lineHeight),
					cursorPaint);
			}

			canvas.Restore();
		}

		public (int lineIndex, int lineOffset, int cursorIndex) GetCursorAtPosition(TextBoxControl control, Vector2 pos)
		{
			var lineIndex = (int)(pos.Y / LineHeight);
			if (lineIndex >= control.Lines.Length)
				return (-1, -1, -1);

			var line = control.Lines[lineIndex];

			var lineOffset = TextUtil.BinarySearchCursor(i => _textPaint.MeasureText(line[..i]), line.Length, pos.X);

			return (lineIndex, lineOffset, control.Lines[..lineIndex].Sum(s => s.Length + 1) + lineOffset);
		}
	}
}