using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Comlink.Util;
using Nedry;
using Nedry.Pin;

namespace Comlink.Controls
{
	public partial class PlayerDialogueEditControl : UserControl
	{
		public static readonly RoutedCommand AddCommand = new();
		public static readonly RoutedCommand DeleteSelectedCommand = new();
		public static readonly RoutedCommand ApplyCommand = new();

		public event EventHandler<Dictionary<UniqueId, string>> ChangesApplied;

		public ObservableCollection<KeyedSelectableWrapper<string>> DialogueOptions { get; set; }

		public PlayerDialogueEditControl(KeyValuePair<UniqueId, IOutputPin>[] node)
		{
			DialogueOptions = new ObservableCollection<KeyedSelectableWrapper<string>>(node.Select(pair => new KeyedSelectableWrapper<string>(pair.Key, pair.Value.Name)));

			InitializeComponent();
		}

		private void AlwaysCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void AddCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			DialogueOptions.Add(new KeyedSelectableWrapper<string>(UniqueId.NewId(), "Dialogue Option"));
		}

		private void DeleteSelectedCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			DialogueOptions.RemoveAll(wrapper => wrapper.Selected);
		}

		private void ApplyCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			OnChangesApplied(DialogueOptions.ToDictionary(wrapper => wrapper.Id, wrapper => wrapper.Value));
		}

		protected virtual void OnChangesApplied(Dictionary<UniqueId, string> e)
		{
			ChangesApplied?.Invoke(this, e);
		}
	}
}