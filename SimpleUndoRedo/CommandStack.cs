using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SimpleUndoRedo
{
	public class CommandStack<T> : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		private readonly Stack<ICommand<T>> _commands;
		private readonly Stack<ICommand<T>> _redoCommands;
		private readonly T _source;
		public bool CanRedo => _redoCommands.Count > 0;

		public bool CanUndo => _commands.Count > 0;

		public CommandStack(T source)
		{
			_source = source;
			_commands = new Stack<ICommand<T>>();
			_redoCommands = new Stack<ICommand<T>>();
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

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}