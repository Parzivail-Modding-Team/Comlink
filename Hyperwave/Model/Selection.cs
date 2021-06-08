using System;

namespace Hyperwave.Model
{
	public record Selection(int StartIndex, int EndIndex, CursorSide CursorSide = CursorSide.End)
	{
		public int Length => EndIndex - StartIndex;

		public Selection(int startIndex) : this(startIndex, startIndex)
		{
		}

		public Selection MoveCursor(CursorDirection direction, int distance)
		{
			switch (direction)
			{
				case CursorDirection.Left:
				{
					if (Length == 0)
						return new Selection(StartIndex - distance);

					if (CursorSide == CursorSide.End)
						return new Selection(StartIndex, EndIndex - distance, CursorSide);
					return new Selection(StartIndex - distance, EndIndex, CursorSide);
				}
				case CursorDirection.Right:
				{
					if (Length == 0)
						return new Selection(EndIndex + distance);

					if (CursorSide == CursorSide.End)
						return new Selection(StartIndex, EndIndex + distance, CursorSide);
					return new Selection(StartIndex + distance, EndIndex, CursorSide);
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
			}
		}
	}
}