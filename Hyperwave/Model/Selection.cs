using System;

namespace Hyperwave.Model
{
	public record Selection(int StartIndex, int EndIndex, CursorSide CursorSide = CursorSide.End)
	{
		public int Cursor => GetSide(CursorSide);
		public int Length => EndIndex - StartIndex;

		public Selection(int startIndex) : this(startIndex, startIndex)
		{
		}

		public int GetSide(CursorSide side)
		{
			return side switch
			{
				CursorSide.Start => StartIndex,
				CursorSide.End => EndIndex,
				_ => throw new ArgumentOutOfRangeException()
			};
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

		public Selection SetEnd(int end)
		{
			if (end <= StartIndex)
			{
				if (CursorSide == CursorSide.End)
					return new Selection(end, StartIndex, CursorSide.Start);
				return new Selection(end, EndIndex, CursorSide.Start);
			}

			if (CursorSide == CursorSide.End)
				return new Selection(StartIndex, end);
			return new Selection(EndIndex, end);
		}
	}
}