using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Comlink.Command
{
	public class CommandStack<T> : INotifyPropertyChanged
	{
		private readonly T _source;
		private readonly Stack<ICommand<T>> _commands;
		private readonly Stack<ICommand<T>> _redoCommands;

		public bool CanUndo => _commands.Count > 0;
		public bool CanRedo => _redoCommands.Count > 0;

		public CommandStack(T source)
		{
			_source = source;
			_commands = new Stack<ICommand<T>>();
			_redoCommands = new Stack<ICommand<T>>();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void Undo()
		{
			if (!CanUndo)
				return;

			var command = _commands.Pop();

			command.Revert(_source);
			_redoCommands.Push(command);

			OnPropertyChanged(nameof(CanUndo));
			OnPropertyChanged(nameof(CanRedo));
		}

		public void Redo()
		{
			if (!CanRedo)
				return;

			var command = _redoCommands.Pop();
			command.Apply(_source);
			_commands.Push(command);

			OnPropertyChanged(nameof(CanUndo));
			OnPropertyChanged(nameof(CanRedo));
		}

		public void ApplyCommand(ICommand<T> command)
		{
			command.Apply(_source);
			_commands.Push(command);

			if (_redoCommands.Count > 0)
				_redoCommands.Clear();

			OnPropertyChanged(nameof(CanUndo));
			OnPropertyChanged(nameof(CanRedo));
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}