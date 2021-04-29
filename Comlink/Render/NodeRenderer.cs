using System;
using Comlink.Extensions;
using Comlink.Model;
using Nedry;
using OpenTK.Mathematics;
using SkiaSharp;

namespace Comlink.Render
{
	public class NodeRenderer
	{
		public float[] SelectionStrokeInterval = {5, 5};
		private readonly SKTypeface _headerTypeface;
		private readonly SKTypeface _nodeTypeface;
		private readonly SKPaint _paint;
		public float NodeCornerRadius { get; set; } = 9;
		public float NodeBorderSize { get; set; } = 3;

		public uint BodyColor { get; set; } = 0xFF_2f4f4f;
		public uint SelectionBorderColor { get; set; } = 0xFF_000000;

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

		public Box2 GetBounds(Node node)
		{
			var x = node.X;
			var y = node.Y;

			var headerTextPaint = _paint.Clone().WithColor(0xFF_FFFFFF).WithTypeface(_headerTypeface);
			var textPaint = _paint.Clone().WithColor(0xFF_FFFFFF).WithTypeface(_nodeTypeface);

			var headerLineHeight = (int) (headerTextPaint.FontMetrics.Descent - headerTextPaint.FontMetrics.Ascent + headerTextPaint.FontMetrics.Leading);

			var lineHeight = (int) (textPaint.FontMetrics.Descent - textPaint.FontMetrics.Ascent + textPaint.FontMetrics.Leading);

			var width = GetNodeWidth(node, headerTextPaint, textPaint);
			var numPins = Math.Max(node.InputPins.Count, node.OutputPins.Count);
			var height = lineHeight * (numPins - 1) - textPaint.FontMetrics.Ascent + textPaint.FontMetrics.Descent + 6;

			var boundsExpansion = NodeBorderSize;

			// Selection bounds is slightly larger than render bounds to make connection hit testing more ergonomic
			return new Box2(x - NodeBorderSize - boundsExpansion, y - headerLineHeight - boundsExpansion, x + width + NodeBorderSize + 2 * boundsExpansion,
				y + height + NodeBorderSize + 2 * boundsExpansion);
		}

		public IPin GetPin(Node node, float testX, float testY)
		{
			var x = node.X;
			var y = node.Y;

			var headerTextPaint = _paint.Clone().WithColor(0xFF_FFFFFF).WithTypeface(_headerTypeface);
			var textPaint = _paint.Clone().WithColor(0xFF_FFFFFF).WithTypeface(_nodeTypeface);

			var lineHeight = (int) (textPaint.FontMetrics.Descent - textPaint.FontMetrics.Ascent + textPaint.FontMetrics.Leading);

			var width = GetNodeWidth(node, headerTextPaint, textPaint);

			y -= (int) (textPaint.FontMetrics.Ascent - 3);

			// Hit-test circle radius larger than render radius
			var circleRadius = lineHeight / 2f;
			var pinOffset = lineHeight / 4f;

			var pY = y;
			for (var i = 0; i < node.InputPins.Count; i++, pY += lineHeight)
			{
				var pin = node.InputPins[i];

				if (BoundsHelper.CircleContainsInclusive(x, pY - pinOffset, circleRadius, testX, testY))
					return pin;
			}

			pY = y;
			for (var i = 0; i < node.OutputPins.Count; i++, pY += lineHeight)
			{
				var pin = node.OutputPins[i];

				if (BoundsHelper.CircleContainsInclusive(x + width, pY - pinOffset, circleRadius, testX, testY))
					return pin;
			}

			return null;
		}

		public Vector2? GetPinPos(Node node, IPin queryPin)
		{
			var x = node.X;
			var y = node.Y;

			var headerTextPaint = _paint.Clone().WithColor(0xFF_FFFFFF).WithTypeface(_headerTypeface);
			var textPaint = _paint.Clone().WithColor(0xFF_FFFFFF).WithTypeface(_nodeTypeface);

			var lineHeight = (int) (textPaint.FontMetrics.Descent - textPaint.FontMetrics.Ascent + textPaint.FontMetrics.Leading);

			var width = GetNodeWidth(node, headerTextPaint, textPaint);

			y -= (int) (textPaint.FontMetrics.Ascent - 3);

			var pinOffset = lineHeight / 4f;

			switch (queryPin)
			{
				case IInputPin inputQueryPin:
				{
					var index = node.InputPins.IndexOf(inputQueryPin);
					if (index == -1)
						return null;

					return new Vector2(x, y - pinOffset + lineHeight * index);
				}
				case IOutputPin outputQueryPin:
				{
					var index = node.OutputPins.IndexOf(outputQueryPin);
					if (index == -1)
						return null;

					return new Vector2(x + width, y - pinOffset + lineHeight * index);
				}
			}

			return null;
		}

		public void DrawNode(SKCanvas ctx, Node node, bool selected)
		{
			var headerTextPaint = _paint.Clone().WithColor(0xFF_FFFFFF).WithTypeface(_headerTypeface);
			var textPaint = _paint.Clone().WithColor(0xFF_FFFFFF).WithTypeface(_nodeTypeface);

			var headerLineHeight = (int) (headerTextPaint.FontMetrics.Descent - headerTextPaint.FontMetrics.Ascent + headerTextPaint.FontMetrics.Leading);
			var headerBaselineOffset = (int) -headerTextPaint.FontMetrics.Ascent;

			var lineHeight = (int) (textPaint.FontMetrics.Descent - textPaint.FontMetrics.Ascent + textPaint.FontMetrics.Leading);

			var width = GetNodeWidth(node, headerTextPaint, textPaint);
			var numPins = Math.Max(node.InputPins.Count, node.OutputPins.Count);
			var height = lineHeight * (numPins - 1) - textPaint.FontMetrics.Ascent + textPaint.FontMetrics.Descent + 6;

			var x = node.X;
			var y = node.Y;

			var headerX = x - NodeBorderSize;
			var headerY = y - headerLineHeight;
			var headerWidth = width + 2 * NodeBorderSize;
			var headerHeight = height + headerLineHeight + NodeBorderSize;

			if (selected)
			{
				var selectionStrokeSize = NodeBorderSize / 2f;

				var paint = _paint.Clone();
				paint.IsStroke = true;
				paint.Color = new SKColor(SelectionBorderColor);
				paint.StrokeWidth = selectionStrokeSize;
				paint.PathEffect = SKPathEffect.CreateDash(SelectionStrokeInterval, (float) ((DateTime.Now - DateTime.Today).TotalSeconds * 10 % 20));

				ctx.DrawRoundRect(headerX - selectionStrokeSize, headerY - selectionStrokeSize, headerWidth + 2 * selectionStrokeSize, headerHeight + 2 * selectionStrokeSize,
					NodeCornerRadius + selectionStrokeSize, NodeCornerRadius + selectionStrokeSize, paint);
			}

			// header
			ctx.DrawRoundRect(headerX, headerY, headerWidth, headerHeight, NodeCornerRadius, NodeCornerRadius, _paint.WithColor(node.Color));

			// title
			ctx.DrawCircle(x + (headerBaselineOffset - NodeBorderSize + 1) / 2f, y - headerLineHeight / 2f, headerLineHeight / 4f, headerTextPaint);
			ctx.DrawText(node.Name, x + headerBaselineOffset, y - headerLineHeight + headerBaselineOffset, headerTextPaint);

			// body
			ctx.DrawRoundRect(x, y, width, height, NodeCornerRadius - NodeBorderSize + 1, NodeCornerRadius - NodeBorderSize + 1, _paint.WithColor(BodyColor));

			y -= (int) (textPaint.FontMetrics.Ascent - 3);

			var triangleRadius = lineHeight / 10f;
			var circleRadius = lineHeight / 3f;
			var pinOffset = lineHeight / 4f;

			var pY = y;
			for (var i = 0; i < node.InputPins.Count; i++, pY += lineHeight)
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

				ctx.DrawText(pin.Name, x + lineHeight / 2f, pY, textPaint);
			}

			pY = y;
			for (var i = 0; i < node.OutputPins.Count; i++, pY += lineHeight)
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
				ctx.DrawText(pin.Name, x + width - textWidth - lineHeight / 2f, pY, textPaint);
			}
		}
	}
}