using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Comlink.Command;
using Comlink.Model;
using Comlink.Model.Nodes;
using Comlink.Project;
using Comlink.Render;
using Comlink.Util;
using ComlinkGtk.GraphicsBindings;
using Gdk;
using Gtk;
using Nedry;
using Nedry.Pin;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SimpleUndoRedo;
using Selection = Gdk.Selection;
using UI = Gtk.Builder.ObjectAttribute;
using Window = Gtk.Window;

namespace ComlinkGtk
{
	internal class MainWindow : Window, IViewport
	{
		private readonly GraphRenderer _graphRenderer;

		private readonly Dictionary<uint, MouseButton> _mouseButtons = new()
		{
			{ 1, MouseButton.Left },
			{ 3, MouseButton.Right }
		};

		private readonly Dictionary<MouseButton, bool> _mouseState = new();
		[UI] private MenuItem _editCopy;
		[UI] private MenuItem _editCut;
		[UI] private MenuItem _editDelete;
		[UI] private MenuItem _editPaste;
		[UI] private MenuItem _editRedo;
		[UI] private MenuItem _editSelectAll;
		[UI] private MenuItem _editSelectInverse;
		[UI] private MenuItem _editSelectNone;

		[UI] private MenuItem _editUndo;
		[UI] private MenuItem _fileExit;

		[UI] private MenuItem _fileNew;
		[UI] private MenuItem _fileOpen;
		[UI] private MenuItem _fileSave;
		[UI] private MenuItem _fileSaveAs;
		private ComlinkProject _loadedProject;
		[UI] private MenuItem _nodesBranch;
		[UI] private MenuItem _nodesConstant;
		[UI] private MenuItem _nodesExit;

		[UI] private MenuItem _nodesInteract;
		[UI] private MenuItem _nodesNpcDialogue;
		[UI] private MenuItem _nodesPlayerDialogue;
		[UI] private MenuItem _nodesTriggerEvent;
		[UI] private MenuItem _nodesVariableGet;
		[UI] private MenuItem _nodesVariableSet;

		[UI] private GLArea _viewport;

		[UI] private MenuItem _viewResetView;

		/// <inheritdoc />
		public int Framebuffer { get; private set; }

		/// <inheritdoc />
		public Graph Graph => _loadedProject.Graph;

		/// <inheritdoc />
		public int Height { get; private set; }

		/// <inheritdoc />
		public int Width { get; private set; }

		public MainWindow() : this(new Builder("comlink.glade"))
		{
		}

		private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
		{
			builder.Autoconnect(this);

			foreach (var value in _mouseButtons.Values)
				_mouseState[value] = false;

			DeleteEvent += Window_DeleteEvent;

			WireMenu();
			WireViewport();

			_loadedProject = ComlinkProject.NewEmptyProject();
			_graphRenderer = new GraphRenderer(this);
			_graphRenderer.CommandExecuted += GraphRendererOnCommandExecuted;
			_graphRenderer.SelectionChanged += SelectedNodesChanged;
		}

		/// <inheritdoc />
		public bool IsKeyDown(Keys key)
		{
			return false;
		}

		private void AddCenteredNode(ComlinkNode node)
		{
			var (centerX, centerY) = _graphRenderer.ControlToBoardCoords(new Vector2(Width / 2f, Height / 2f));

			node.X = centerX;
			node.Y = centerY;

			_loadedProject.CommandStack.ApplyCommand(new CreateNodeCommand(node));
		}

		private static FileFilter CreateFileFilter()
		{
			var f = new FileFilter
			{
				Name = "Comlink Projects"
			};
			f.AddPattern("*.cmlk");
			return f;
		}

		private void EditCopyOnActivated(object sender, EventArgs e)
		{
			using var ms = new MemoryStream();

			var selectedNodes = _graphRenderer.Selection.Where(node => node.NodeType != NodeType.Interact).ToArray();

			var bw = new BinaryWriter(ms);
			bw.Write(selectedNodes.Length);
			foreach (var node in selectedNodes)
				node.Serialize(bw);

			var clipboard = _viewport.GetClipboard(Selection.Clipboard);

			clipboard.Text = Convert.ToBase64String(ms.ToArray());
		}

		private void EditCutOnActivated(object sender, EventArgs e)
		{
			EditCopyOnActivated(sender, e);
			EditDeleteOnActivated(sender, e);
		}

		private void EditDeleteOnActivated(object sender, EventArgs e)
		{
			foreach (var node in _graphRenderer.Selection)
			{
				var connections = _loadedProject.Graph
					.SelectMany(n => n
						.Connections
						.Where(connection =>
							connection.Source.Node == node.NodeId || connection.Destination.Node == node.NodeId)
					).ToArray();
				if (connections.Length > 0)
					_loadedProject.CommandStack.ApplyCommand(new DeleteConnectionsCommand(connections));
			}

			_loadedProject.CommandStack.ApplyCommand(new DeleteNodesCommand(_graphRenderer.Selection.ToArray()));

			_graphRenderer.Selection.Clear();

			// TODO
			// NodePropsControl.Content = null;
		}

		private void EditPasteOnActivated(object sender, EventArgs e)
		{
			var clipboard = _viewport.GetClipboard(Selection.Clipboard);
			clipboard.RequestText((clipboard1, text) => { PasteNodesFromBytes(Convert.FromBase64String(text)); });
		}

		private void EditRedoOnActivated(object sender, EventArgs e)
		{
			_loadedProject.CommandStack.Redo();
		}

		private void EditSelectAllOnActivated(object sender, EventArgs e)
		{
			_graphRenderer.SelectAll();
		}

		private void EditSelectInverseOnActivated(object sender, EventArgs e)
		{
			_graphRenderer.SelectInverse();
		}

		private void EditSelectNoneOnActivated(object sender, EventArgs e)
		{
			_graphRenderer.SelectNone();
		}

		private void EditUndoOnActivated(object sender, EventArgs e)
		{
			_loadedProject.CommandStack.Undo();
		}

		private void FileExitOnActivated(object sender, EventArgs e)
		{
			// TODO
		}

		private void FileNewOnActivated(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		private void FileOpenOnActivated(object sender, EventArgs e)
		{
			var filechooser = new FileChooserDialog(
				"Open Project",
				this,
				FileChooserAction.Open,
				"Cancel", ResponseType.Cancel,
				"Open", ResponseType.Accept
			)
			{
				Filter = CreateFileFilter()
			};

			if (filechooser.Run() == (int)ResponseType.Accept)
				_loadedProject = ComlinkProject.Load(filechooser.Filename);

			filechooser.Destroy();
		}

		private void FileSaveAsOnActivated(object sender, EventArgs e)
		{
			var filechooser = new FileChooserDialog(
				"Save Project",
				this,
				FileChooserAction.Save,
				"Cancel", ResponseType.Cancel,
				"Save", ResponseType.Accept
			)
			{
				Filter = CreateFileFilter()
			};

			if (filechooser.Run() == (int)ResponseType.Accept)
			{
				var filename = filechooser.Filename;
				if (System.IO.Path.GetExtension(filename) != ".cmlk")
					filename += ".cmlk";

				_loadedProject.Save(filename);
			}

			filechooser.Destroy();
		}

		private void FileSaveOnActivated(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		private void GraphRendererOnCommandExecuted(object sender, ICommand<Graph> e)
		{
			_loadedProject.CommandStack.ApplyCommand(e);
		}

		private void NodesBranchOnActivated(object sender, EventArgs e)
		{
			AddCenteredNode(new BranchNode());
		}

		private void NodesConstantOnActivated(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		private void NodesExitOnActivated(object sender, EventArgs e)
		{
			AddCenteredNode(new ExitNode());
		}

		private void NodesInteractOnActivated(object sender, EventArgs e)
		{
			if (_loadedProject.Graph.Any(node => node.NodeType == NodeType.Interact))
				Warn("Only one Interact node can be present in a graph.");
			else
				AddCenteredNode(new InteractNode());
		}

		private void NodesNpcDialogueOnActivated(object sender, EventArgs e)
		{
			AddCenteredNode(new NpcDialogueNode());
		}

		private void NodesPlayerDialogueOnActivated(object sender, EventArgs e)
		{
			AddCenteredNode(new PlayerDialogueNode());
		}

		private void NodesTriggerEventOnActivated(object sender, EventArgs e)
		{
			AddCenteredNode(new TriggerEventNode("event"));
		}

		private void NodesVariableGetOnActivated(object sender, EventArgs e)
		{
			AddCenteredNode(new NpcDialogueNode());
		}

		private void NodesVariableSetOnActivated(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		private void PasteNodesFromBytes(byte[] data)
		{
			using var nodeStream = new MemoryStream(data);
			var br = new BinaryReader(nodeStream);
			var numNodes = br.ReadInt32();

			var nodes = new List<ComlinkNode>();
			for (var i = 0; i < numNodes; i++)
				nodes.Add(ComlinkNode.Deserialize(br));

			var nodeIdMap = new Dictionary<UniqueId, UniqueId>();

			// give each node a new ID
			foreach (var node in nodes)
			{
				var newId = UniqueId.NewId();
				nodeIdMap[node.NodeId] = newId;

				node.NodeId = newId;
			}

			// remap the pin IDs and connections to the new IDs,
			// or remove them if the referenced node wasn't copied

			var averageX = nodes.Average(node => node.X);
			var averageY = nodes.Average(node => node.Y);

			var newAverage = _graphRenderer.GetViewportCenter();

			foreach (var node in nodes)
			{
				node.Connections.RemoveAll(connection =>
					!(nodeIdMap.ContainsKey(connection.Source.Node) &&
					  nodeIdMap.ContainsKey(connection.Destination.Node)));
				foreach (var connection in node.Connections)
				{
					connection.Source = new PinId(nodeIdMap[connection.Source.Node], connection.Source.GetPinBytes());
					connection.Destination = new PinId(nodeIdMap[connection.Destination.Node],
						connection.Destination.GetPinBytes());
				}

				foreach (var pin in node.InputPins)
					pin.PinId = new PinId(node.NodeId, pin.PinId.GetPinBytes());

				foreach (var pin in node.OutputPins)
					pin.PinId = new PinId(node.NodeId, pin.PinId.GetPinBytes());

				var offsetFromAverageX = node.X - averageX;
				var offsetFromAverageY = node.Y - averageY;

				node.X = offsetFromAverageX + newAverage.X;
				node.Y = offsetFromAverageY + newAverage.Y;

				_loadedProject.CommandStack.ApplyCommand(new CreateNodeCommand(node));
			}
		}

		private void SelectedNodesChanged(object sender, EventArgs eventArgs)
		{
			// TODO
		}

		private void ViewportCreated(object sender, EventArgs e)
		{
			_viewport.MakeCurrent();

			GL.LoadBindings(new NativeBindingsContext());
		}

		private void ViewportOnButtonPressEvent(object o, ButtonPressEventArgs args)
		{
			if (!_mouseButtons.ContainsKey(args.Event.Button))
				return;

			var button = _mouseButtons[args.Event.Button];
			_mouseState[button] = true;

			_graphRenderer.OnMouseDown(button, new Vector2((float)args.Event.X, (float)args.Event.Y));
		}

		private void ViewportOnButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
		{
			if (!_mouseButtons.ContainsKey(args.Event.Button))
				return;

			var button = _mouseButtons[args.Event.Button];
			_mouseState[button] = false;

			_graphRenderer.OnMouseUp(button, new Vector2((float)args.Event.X, (float)args.Event.Y));
		}

		private void ViewportOnMotionNotifyEvent(object o, MotionNotifyEventArgs args)
		{
			_graphRenderer.OnMouseMove(new Vector2((float)args.Event.X, (float)args.Event.Y),
				_mouseState[MouseButton.Left], _mouseState[MouseButton.Right]);
		}

		private void ViewportOnRender(object sender, RenderArgs e)
		{
			_viewport.MakeCurrent();
			Framebuffer = GL.GetInteger(GetPName.FramebufferBinding);

			_graphRenderer.OnRender();

			_viewport.QueueDraw();
		}

		private void ViewportOnScrollEvent(object o, ScrollEventArgs args)
		{
			_graphRenderer.OnMouseWheel(args.Event.Direction == ScrollDirection.Up ? 1 : -1,
				new Vector2((float)args.Event.X, (float)args.Event.Y));
		}

		private void ViewResetViewOnActivated(object sender, EventArgs e)
		{
			_graphRenderer.ResetTransform();
		}

		private void Warn(string message)
		{
			throw new NotImplementedException();
		}

		private void Window_DeleteEvent(object sender, DeleteEventArgs a)
		{
			Application.Quit();
		}

		private void WireMenu()
		{
			_fileNew.Activated += FileNewOnActivated;
			_fileOpen.Activated += FileOpenOnActivated;
			_fileSave.Activated += FileSaveOnActivated;
			_fileSaveAs.Activated += FileSaveAsOnActivated;
			_fileExit.Activated += FileExitOnActivated;

			_editUndo.Activated += EditUndoOnActivated;
			_editRedo.Activated += EditRedoOnActivated;
			_editCut.Activated += EditCutOnActivated;
			_editCopy.Activated += EditCopyOnActivated;
			_editPaste.Activated += EditPasteOnActivated;
			_editDelete.Activated += EditDeleteOnActivated;
			_editSelectAll.Activated += EditSelectAllOnActivated;
			_editSelectNone.Activated += EditSelectNoneOnActivated;
			_editSelectInverse.Activated += EditSelectInverseOnActivated;

			_viewResetView.Activated += ViewResetViewOnActivated;

			_nodesInteract.Activated += NodesInteractOnActivated;
			_nodesExit.Activated += NodesExitOnActivated;
			_nodesPlayerDialogue.Activated += NodesPlayerDialogueOnActivated;
			_nodesNpcDialogue.Activated += NodesNpcDialogueOnActivated;
			_nodesVariableGet.Activated += NodesVariableGetOnActivated;
			_nodesVariableSet.Activated += NodesVariableSetOnActivated;
			_nodesConstant.Activated += NodesConstantOnActivated;
			_nodesBranch.Activated += NodesBranchOnActivated;
			_nodesTriggerEvent.Activated += NodesTriggerEventOnActivated;
		}

		private void WireViewport()
		{
			_viewport.AddEvents((int)(
				EventMask.ButtonPressMask |
				EventMask.ButtonReleaseMask |
				EventMask.PointerMotionMask |
				EventMask.ScrollMask |
				EventMask.KeyPressMask |
				EventMask.KeyReleaseMask
			));

			_viewport.ButtonPressEvent += ViewportOnButtonPressEvent;
			_viewport.ButtonReleaseEvent += ViewportOnButtonReleaseEvent;
			_viewport.MotionNotifyEvent += ViewportOnMotionNotifyEvent;
			_viewport.Realized += ViewportCreated;
			_viewport.Render += ViewportOnRender;
			_viewport.Resize += (o, args) =>
			{
				Width = args.Width;
				Height = args.Height;
			};
			_viewport.ScrollEvent += ViewportOnScrollEvent;
		}
	}
}