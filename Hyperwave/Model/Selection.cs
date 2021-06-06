namespace Hyperwave.Model
{
	public record Selection(int StartIndex, int EndIndex, CursorSide CursorSide = CursorSide.End)
	{
		public int Length => EndIndex - StartIndex;

		public Selection(int startIndex) : this(startIndex, startIndex)
		{
		}
	}
}