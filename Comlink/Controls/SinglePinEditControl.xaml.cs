using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace Comlink.Controls
{
	public partial class SinglePinEditControl : UserControl
	{
		public static readonly RoutedCommand ApplyCommand = new();

		public event EventHandler<string> ChangesApplied;

		public string ValueTitle { get; set; }
		public string Value { get; set; }

		public SinglePinEditControl(string title, string existingValue)
		{
			ValueTitle = title;
			Value = existingValue;

			InitializeComponent();
		}

		private void AlwaysCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void ApplyCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			OnChangesApplied(Value);
		}

		protected virtual void OnChangesApplied(string e)
		{
			ChangesApplied?.Invoke(this, e);
		}
	}
}