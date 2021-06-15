using System;
using System.Threading;

namespace Hyperwave.Helper
{
	public static class CursorManager
	{
		private static readonly Timer BlinkTimer = new(ToggleBlinkPeriod);
		private static double _blinkInterval;


		public static double CursorBlinkPeriod
		{
			get => _blinkInterval;
			set
			{
				_blinkInterval = value;
				ResetBlinking();
			}
		}

		public static bool CursorBlinkPhase { get; private set; }

		static CursorManager()
		{
			CursorBlinkPeriod = 600;
		}

		public static void ResetBlinking()
		{
			CursorBlinkPhase = true;

			var interval = TimeSpan.FromMilliseconds(_blinkInterval);
			BlinkTimer.Change(interval, interval);
		}

		private static void ToggleBlinkPeriod(object state)
		{
			CursorBlinkPhase = !CursorBlinkPhase;
		}
	}
}