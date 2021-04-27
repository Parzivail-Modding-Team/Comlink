using System;
using Comlink.Extensions;
using Nedry;
using SkiaSharp;

namespace Comlink.Render
{
	public class NodeRenderer
	{
		private readonly SKTypeface _headerTypeface;
		private readonly SKTypeface _nodeTypeface;
		private readonly SKPaint _paint;

		public float NodeCornerRadius { get; set; } = 9;
		public float NodeBorderSize { get; set; } = 3;

		public uint BodyColor { get; set; } = 0xFF_2f4f4f;

		public NodeRenderer(SKPaint basePaint, SKTypeface headerTypeface, SKTypeface nodeTypeface)
		{
			_paint = basePaint;
			_headerTypeface = headerTypeface;
			_nodeTypeface = nodeTypeface;
		}

		private float GetNodeWidth(Node node, SKPaint headerTextPaint, SKPaint textPaint)
		{
			return 200;
		}

		public bool NodeContains(int x, int y, Node node, float testX, float testY)
		{
			var headerTextPaint = _paint.Clone().WithColor(0xFF_FFFFFF).WithTypeface(_headerTypeface);
			var textPaint = _paint.Clone().WithColor(0xFF_FFFFFF).WithTypeface(_nodeTypeface);

			var headerLineHeight = (int) (headerTextPaint.FontMetrics.Descent - headerTextPaint.FontMetrics.Ascent + headerTextPaint.FontMetrics.Leading);

			var lineHeight = (int) (textPaint.FontMetrics.Descent - textPaint.FontMetrics.Ascent + textPaint.FontMetrics.Leading);

			var width = GetNodeWidth(node, headerTextPaint, textPaint);
			var numPins = Math.Max(node.InputPins.Length, node.OutputPins.Length);
			var height = lineHeight * (numPins - 1) - textPaint.FontMetrics.Ascent + textPaint.FontMetrics.Descent + 6;

			return BoundsHelper.RectContainsInclusive(x - NodeBorderSize, y - headerLineHeight, width + 2 * NodeBorderSize, height + headerLineHeight + NodeBorderSize, testX, testY);
		}

		public IPin GetPin(int x, int y, Node node, float testX, float testY)
		{
			var headerTextPaint = _paint.Clone().WithColor(0xFF_FFFFFF).WithTypeface(_headerTypeface);
			var textPaint = _paint.Clone().WithColor(0xFF_FFFFFF).WithTypeface(_nodeTypeface);

			var lineHeight = (int) (textPaint.FontMetrics.Descent - textPaint.FontMetrics.Ascent + textPaint.FontMetrics.Leading);

			var width = GetNodeWidth(node, headerTextPaint, textPaint);

			y -= (int) (textPaint.FontMetrics.Ascent - 3);

			var circleRadius = lineHeight / 3f;
			var pinOffset = lineHeight / 4f;

			for (int i = 0, pY = y; i < node.InputPins.Length; i++, pY += lineHeight)
			{
				var pin = node.InputPins[i];

				if (BoundsHelper.CircleContainsInclusive(x, pY - pinOffset, circleRadius, testX, testY))
					return pin;
			}

			for (int i = 0, pY = y; i < node.OutputPins.Length; i++, pY += lineHeight)
			{
				var pin = node.OutputPins[i];

				if (BoundsHelper.CircleContainsInclusive(x + width, pY - pinOffset, circleRadius, testX, testY))
					return pin;
			}

			return null;
		}

		public void DrawNode(SKCanvas ctx, int x, int y, Node node)
		{
			var headerTextPaint = _paint.Clone().WithColor(0xFF_FFFFFF).WithTypeface(_headerTypeface);
			var textPaint = _paint.Clone().WithColor(0xFF_FFFFFF).WithTypeface(_nodeTypeface);

			var headerLineHeight = (int) (headerTextPaint.FontMetrics.Descent - headerTextPaint.FontMetrics.Ascent + headerTextPaint.FontMetrics.Leading);
			var headerBaselineOffset = (int) -headerTextPaint.FontMetrics.Ascent;

			var lineHeight = (int) (textPaint.FontMetrics.Descent - textPaint.FontMetrics.Ascent + textPaint.FontMetrics.Leading);

			var width = GetNodeWidth(node, headerTextPaint, textPaint);
			var numPins = Math.Max(node.InputPins.Length, node.OutputPins.Length);
			var height = lineHeight * (numPins - 1) - textPaint.FontMetrics.Ascent + textPaint.FontMetrics.Descent + 6;

			// header
			ctx.DrawRoundRect(x - NodeBorderSize, y - headerLineHeight, width + 2 * NodeBorderSize, height + headerLineHeight + NodeBorderSize, NodeCornerRadius, NodeCornerRadius,
				_paint.WithColor(node.Color));

			// title
			ctx.DrawCircle(x + (headerBaselineOffset - NodeBorderSize + 1) / 2f, y - headerLineHeight / 2f, headerLineHeight / 4f, headerTextPaint);
			ctx.DrawText(node.Name, x + headerBaselineOffset, y - headerLineHeight + headerBaselineOffset, headerTextPaint);

			// body
			ctx.DrawRoundRect(x, y, width, height, NodeCornerRadius - NodeBorderSize + 1, NodeCornerRadius - NodeBorderSize + 1, _paint.WithColor(BodyColor));

			y -= (int) (textPaint.FontMetrics.Ascent - 3);

			var triangleRadius = lineHeight / 10f;
			var circleRadius = lineHeight / 3f;
			var pinOffset = lineHeight / 4f;

			for (int i = 0, pY = y; i < node.InputPins.Length; i++, pY += lineHeight)
			{
				var pin = node.InputPins[i];

				if (pin is FlowInputPin)
				{
					ctx.DrawRoundedTriangle(x - pinOffset / 2, pY - pinOffset, pinOffset, triangleRadius + NodeBorderSize, _paint.WithColor(BodyColor));
					ctx.DrawRoundedTriangle(x - pinOffset / 2, pY - pinOffset, pinOffset, triangleRadius, _paint.WithColor(pin.Color));
				}
				else
				{
					// input dot
					ctx.DrawCircle(x, pY - pinOffset, circleRadius, _paint.WithColor(BodyColor));
					ctx.DrawCircle(x, pY - pinOffset, circleRadius - NodeBorderSize, _paint.WithColor(pin.Color));
				}

				ctx.DrawText(pin.Name, (int) (x + lineHeight / 2f), pY, textPaint);
			}

			for (int i = 0, pY = y; i < node.OutputPins.Length; i++, pY += lineHeight)
			{
				var pin = node.OutputPins[i];

				if (pin is FlowOutputPin)
				{
					ctx.DrawRoundedTriangle(x + width - pinOffset / 2, pY - pinOffset, pinOffset, triangleRadius + NodeBorderSize, _paint.WithColor(BodyColor));
					ctx.DrawRoundedTriangle(x + width - pinOffset / 2, pY - pinOffset, pinOffset, triangleRadius, _paint.WithColor(pin.Color));
				}
				else
				{
					// output dot
					ctx.DrawCircle(x + width, pY - pinOffset, circleRadius, _paint.WithColor(BodyColor));
					ctx.DrawCircle(x + width, pY - pinOffset, circleRadius - 3, _paint.WithColor(pin.Color));
				}

				var textWidth = textPaint.MeasureText(pin.Name);
				ctx.DrawText(pin.Name, (int) (x + width - textWidth - lineHeight / 2f), pY, textPaint);
			}
		}
	}
}