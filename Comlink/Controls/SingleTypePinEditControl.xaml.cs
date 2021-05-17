using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;

namespace Comlink.Controls
{
	public partial class SingleTypePinEditControl : UserControl
	{
		public static readonly RoutedCommand ApplyCommand = new();

		public event EventHandler<KeyValuePair<string, string>> ChangesApplied;

		public string ValueTitle { get; set; }
		public string Value { get; set; }
		public string Type { get; set; }

		public SingleTypePinEditControl(string title, string value, string type)
		{
			ValueTitle = title;
			Value = value;
			Type = type;

			InitializeComponent();
		}

		private void AlwaysCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void ApplyCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			OnChangesApplied(Type, Value);
		}

		protected virtual void OnChangesApplied(string type, string value)
		{
			ChangesApplied?.Invoke(this, new KeyValuePair<string, string>(type, value));
		}
	}
}