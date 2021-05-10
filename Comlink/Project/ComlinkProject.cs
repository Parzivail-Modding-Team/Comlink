using System;
using Comlink.Command;
using Comlink.Render;

namespace Comlink.Project
{
	public class ComlinkProject
	{
		public Graph Graph { get; }
		public CommandStack<Graph> CommandStack { get; }

		public ComlinkProject(Graph graph, CommandStack<Graph> commandStack)
		{
			Graph = graph;
			CommandStack = commandStack;
		}

		public static ComlinkProject NewEmptyProject()
		{
			var graph = new Graph();
			return new ComlinkProject(graph, new CommandStack<Graph>(graph));
		}

		public static ComlinkProject Load(string filename)
		{
			throw new NotImplementedException();
		}

		public void Save(string filename)
		{
			throw new NotImplementedException();
		}
	}
}