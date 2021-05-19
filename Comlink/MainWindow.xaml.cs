﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Comlink.Command;
using Comlink.Controls;
using Comlink.Model;
using Comlink.Model.Nodes;
using Comlink.Project;
using Comlink.Render;
using Microsoft.Win32;
using ModernWpf.Controls;
using Nedry;
using Nedry.Pin;
using OpenTK.Wpf;

namespace Comlink
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : INotifyPropertyChanged
	{
		public static readonly RoutedCommand SelectInverse = new();
		public static readonly RoutedCommand SelectNone = new();

		public static readonly RoutedCommand CreateNode = new();

		private readonly GraphRenderer _graphRenderer;

		private ComlinkProject _loadedProject;

		public event PropertyChangedEventHandler PropertyChanged;

		public bool OneSelection => _graphRenderer != null && _graphRenderer.Selection.Count == 1;
		public ComlinkNode SelectedNode => _graphRenderer?.Selection.FirstOrDefault();

		public ComlinkProject LoadedProject
		{
			get => _loadedProject;
			set
			{
				_loadedProject = value;
				if (_graphRenderer != null)
					_graphRenderer.TargetGraph = _loadedProject.Graph;
				OnPropertyChanged();
			}
		}

		static MainWindow()
		{
			SelectInverse.InputGestures.Add(new KeyGesture(Key.I, ModifierKeys.Control | ModifierKeys.Shift));
			SelectNone.InputGestures.Add(new KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Shift));
		}

		public MainWindow()
		{
			InitializeComponent();

			var settings = new GLWpfControlSettings
			{
				MajorVersion = 3,
				MinorVersion = 1,
				RenderContinuously = true
			};
			Viewport.Start(settings);

			LoadedProject = ComlinkProject.NewEmptyProject();
			_graphRenderer = new GraphRenderer(LoadedProject.Graph, Viewport);
			_graphRenderer.CommandExecuted += GraphRendererOnCommandExecuted;
			_graphRenderer.SelectionChanged += SelectedNodesChanged;
		}

		private void SelectedNodesChanged(object sender, EventArgs eventArgs)
		{
			OnPropertyChanged(nameof(OneSelection));
			OnPropertyChanged(nameof(SelectedNode));

			var node = SelectedNode;
			if (node == null)
				return;

			switch (node.NodeType)
			{
				case NodeType.Interact:
				case NodeType.Exit:
				case NodeType.Branch:
					NodePropsControl.Content = null;
					break;
				case NodeType.PlayerDialogue:
				{
					var uidToOldPinMap = node.OutputPins.Select(pin => new KeyValuePair<UniqueId, IOutputPin>(UniqueId.NewId(), pin)).ToArray();
					var oldConnectionMap = node.Connections.Select(connection =>
						new KeyValuePair<Connection, UniqueId>(connection, uidToOldPinMap.First(pair => pair.Value.PinId == connection.Source).Key)).ToArray();

					var control = new PlayerDialogueEditControl(uidToOldPinMap);
					control.ChangesApplied += (ctrl, newOptions) =>
					{
						short index = 0;
						var uidToNewPinMap = newOptions.Select(pair =>
							new KeyValuePair<UniqueId, IOutputPin>(pair.Key, new FlowOutputPin(PinId.NewId(node.NodeId, PinType.Output, index++), pair.Value))).ToArray();

						// remap pin IDs
						var outputPins = uidToNewPinMap.Select(pair => pair.Value).ToArray();

						// remap old connections if possible
						var connections = new List<Connection>();
						foreach (var (connection, sourceUid) in oldConnectionMap)
						{
							if (uidToNewPinMap.All(m => m.Key != sourceUid)) continue;

							var source = uidToNewPinMap.FirstOrDefault(m => m.Key == sourceUid);
							connections.Add(new Connection(source.Value.PinId, connection.Destination));
						}

						_loadedProject.CommandStack.ApplyCommand(new SetOutputsAndConnectionsCommand(node, outputPins, connections.ToArray()));
					};
					NodePropsControl.Content = control;
					break;
				}
				case NodeType.NpcDialogue:
				case NodeType.TriggerEvent:
				case NodeType.ConstantRead:
				{
					var pin = node.OutputPins[0];
					var control = new SinglePinEditControl(node.Name, pin.Name);

					control.ChangesApplied += (o, s) => _loadedProject.CommandStack.ApplyCommand(new SetPinValueCommand(node.NodeId, pin.PinId, pin.Name, s));

					NodePropsControl.Content = control;
					break;
				}
				case NodeType.VariableSet:
				case NodeType.VariableGet:
				{
					NodePropsControl.Content = null;
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void GraphRendererOnCommandExecuted(object sender, ICommand<Graph> e)
		{
			_loadedProject.CommandStack.ApplyCommand(e);
		}

		private void Viewport_OnMouseMove(object sender, MouseEventArgs e)
		{
			_graphRenderer.OnMouseMove(e);
		}

		private void Viewport_OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			_graphRenderer.OnMouseDown(e);
		}

		private void Viewport_OnMouseUp(object sender, MouseButtonEventArgs e)
		{
			_graphRenderer.OnMouseUp(e);
		}

		private void Viewport_OnMouseWheel(object sender, MouseWheelEventArgs e)
		{
			_graphRenderer.OnMouseWheel(e);
		}

		private void Viewport_OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			_graphRenderer.OnSizeChanged(e);
		}

		private void Viewport_OnRender(TimeSpan obj)
		{
			_graphRenderer.OnRender(obj);
		}

		private void AlwaysCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void ProjectOpenCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = LoadedProject != null;
		}

		private void HasSelectionCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _graphRenderer.HasSelection;
		}

		private void NewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			// TODO
		}

		private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			// TODO: confirm closing current project

			var ofd = new OpenFileDialog
			{
				Filter = "Comlink Project|*.cmlk"
			};

			if (!(ofd.ShowDialog() ?? false)) return;

			LoadedProject = ComlinkProject.Load(ofd.FileName);
		}

		private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			// TODO
		}

		private void SaveAsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			var sfd = new SaveFileDialog
			{
				Filter = "Comlink Project|*.cmlk"
			};

			if (!(sfd.ShowDialog() ?? false)) return;

			_loadedProject.Save(sfd.FileName);
		}

		private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			// TODO
		}

		private void UndoCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = LoadedProject.CommandStack.CanUndo;
		}

		private void UndoCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_loadedProject.CommandStack.Undo();
		}

		private void RedoCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = LoadedProject.CommandStack.CanRedo;
		}

		private void RedoCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_loadedProject.CommandStack.Redo();
		}

		private void CutCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			// TODO
		}

		private void CopyCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			// TODO
		}

		private void PasteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			// TODO
		}

		private void DeleteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			foreach (var node in _graphRenderer.Selection)
			{
				var connections = _loadedProject.Graph
					.SelectMany(n => n
						.Connections
						.Where(connection => connection.Source.Node == node.NodeId || connection.Destination.Node == node.NodeId)
					).ToArray();
				if (connections.Length > 0)
					_loadedProject.CommandStack.ApplyCommand(new DeleteConnectionsCommand(connections));
			}

			_loadedProject.CommandStack.ApplyCommand(new DeleteNodesCommand(_graphRenderer.Selection.ToArray()));

			_graphRenderer.Selection.Clear();
			NodePropsControl.Content = null;
		}

		private void SelectAllCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_graphRenderer.SelectAll();
		}

		private void SelectNoneCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_graphRenderer.SelectNone();
		}

		private void SelectInverseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			_graphRenderer.SelectInverse();
		}

		private async void CreateNodeCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (e.Parameter is not NodeType nodeType)
				return;

			var (centerX, centerY) = _graphRenderer.ControlToBoardCoords((float) (Viewport.ActualWidth / 2), (float) (Viewport.ActualHeight / 2));

			switch (nodeType)
			{
				case NodeType.Interact:
				{
					if (_loadedProject.Graph.Any(node => node.NodeType == NodeType.Interact))
					{
						Warn("Only one Interact node can be present in a graph.");
					}
					else
					{
						var node = new InteractNode
						{
							X = centerX,
							Y = centerY
						};
						_loadedProject.CommandStack.ApplyCommand(new CreateNodeCommand(node));
					}

					break;
				}
				case NodeType.Exit:
				{
					var node = new ExitNode
					{
						X = centerX,
						Y = centerY
					};
					_loadedProject.CommandStack.ApplyCommand(new CreateNodeCommand(node));
					break;
				}
				case NodeType.Branch:
				{
					var node = new BranchNode
					{
						X = centerX,
						Y = centerY
					};
					_loadedProject.CommandStack.ApplyCommand(new CreateNodeCommand(node));
					break;
				}
				case NodeType.PlayerDialogue:
				{
					var node = new PlayerDialogueNode
					{
						X = centerX,
						Y = centerY
					};
					_loadedProject.CommandStack.ApplyCommand(new CreateNodeCommand(node));
					break;
				}
				case NodeType.NpcDialogue:
				{
					var node = new NpcDialogueNode
					{
						X = centerX,
						Y = centerY
					};
					_loadedProject.CommandStack.ApplyCommand(new CreateNodeCommand(node));
					break;
				}
				case NodeType.VariableGet:
				{
					var dialog = new SingleTypeNodeAddDialog("Variable Get", "", "");
					var result = await dialog.ShowAsync();

					if (result == ContentDialogResult.Primary)
					{
						var node = new VariableGetNode(dialog.Value, dialog.Type)
						{
							X = centerX,
							Y = centerY
						};
						_loadedProject.CommandStack.ApplyCommand(new CreateNodeCommand(node));
					}

					break;
				}
				case NodeType.VariableSet:
				{
					var dialog = new SingleTypeNodeAddDialog("Variable Set", "", "");
					var result = await dialog.ShowAsync();

					if (result == ContentDialogResult.Primary)
					{
						var node = new VariableSetNode(dialog.Value, dialog.Type)
						{
							X = centerX,
							Y = centerY
						};
						_loadedProject.CommandStack.ApplyCommand(new CreateNodeCommand(node));
					}

					break;
				}
				case NodeType.ConstantRead:
				{
					var dialog = new SingleTypeNodeAddDialog("Constant Read", "", "");
					var result = await dialog.ShowAsync();

					if (result == ContentDialogResult.Primary)
					{
						var node = new ConstantReadNode(dialog.Value, dialog.Type)
						{
							X = centerX,
							Y = centerY
						};
						_loadedProject.CommandStack.ApplyCommand(new CreateNodeCommand(node));
					}

					break;
				}
				case NodeType.TriggerEvent:
				{
					var node = new TriggerEventNode("event")
					{
						X = centerX,
						Y = centerY
					};
					_loadedProject.CommandStack.ApplyCommand(new CreateNodeCommand(node));
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void Warn(string message)
		{
			MessageBox.Show(message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void MenuItem_OnClick(object sender, RoutedEventArgs e)
		{
			((MenuItem) sender).IsSubmenuOpen = true;
		}
	}
}