using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Comlink.Model.Nodes;

namespace Comlink.Controls
{
	public partial class PlayerDialogueEditControl : UserControl
	{
		public static readonly RoutedCommand AddCommand = new();
		public static readonly RoutedCommand DeleteSelectedCommand = new();
		public static readonly RoutedCommand ApplyCommand = new();

		public PlayerDialogueNode Node { get; }

		public ObservableCollection<SelectableWrapper<string>> DialogueOptions { get; set; }

		public PlayerDialogueEditControl(PlayerDialogueNode node)
		{
			Node = node;
			DialogueOptions = new ObservableCollection<SelectableWrapper<string>>(node.Select(s => new SelectableWrapper<string>(s)));
			InitializeComponent();
		}

		private void AlwaysCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void AddCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			DialogueOptions.Add(new SelectableWrapper<string>("Dialogue Option"));
		}

		private void DeleteSelectedCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			throw new NotImplementedException();
		}

		private void ApplyCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			throw new NotImplementedException();
		}
	}
}