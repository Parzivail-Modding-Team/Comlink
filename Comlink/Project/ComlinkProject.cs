using System;
using System.IO;
using System.Text;
using Comlink.Model;
using Comlink.Render;
using SimpleUndoRedo;

namespace Comlink.Project
{
	public class ComlinkProject
	{
		private const string FileMagic = "COMLINK";
		public CommandStack<Graph> CommandStack { get; }

		public Graph Graph { get; }

		public ComlinkProject(Graph graph, CommandStack<Graph> commandStack)
		{
			Graph = graph;
			CommandStack = commandStack;
		}

		public static ComlinkProject Load(string filename)
		{
			using var br = new BinaryReader(File.Open(filename, FileMode.Open));

			var magic = Encoding.ASCII.GetString(br.ReadBytes(FileMagic.Length));
			if (magic != FileMagic)
				throw new InvalidDataException();

			var version = br.ReadInt32();

			var flags = (ProjectFileFlags)br.ReadByte();

			var numNodes = br.ReadInt32();

			var graph = new Graph();
			for (var i = 0; i < numNodes; i++)
				graph.Add(ComlinkNode.Deserialize(br));

			return new ComlinkProject(graph, new CommandStack<Graph>(graph));
		}

		public static ComlinkProject NewEmptyProject()
		{
			var graph = new Graph();
			return new ComlinkProject(graph, new CommandStack<Graph>(graph));
		}

		public void Save(string filename)
		{
			using var bw = new BinaryWriter(File.Open(filename, FileMode.Create));

			bw.Write(Encoding.ASCII.GetBytes(FileMagic));
			bw.Write(1);

			const ProjectFileFlags flags = ProjectFileFlags.HasGraph;

			bw.Write((byte)flags);

			bw.Write(Graph.Count);

			foreach (var node in Graph)
				node.Serialize(bw);
		}

		[Flags]
		private enum ProjectFileFlags : byte
		{
			HasGraph = 0b1,
			HasHistory = 0b10
		}
	}
}