using System;
using System.Runtime.InteropServices;
using OpenTK;

namespace ComlinkGtk.GraphicsBindings
{
	public class GlxBindingsContext : IBindingsContext
	{
		public IntPtr GetProcAddress(string procName)
		{
			return glXGetProcAddress(procName);
		}

		[DllImport("libGL", CharSet = CharSet.Ansi)]
		private static extern IntPtr glXGetProcAddress(string procName);
	}
}