using System;
using System.Runtime.InteropServices;
using OpenTK;

namespace ComlinkGtk.GraphicsBindings
{
	public class NativeBindingsContext : IBindingsContext
	{
		private static IBindingsContext _context;

		public NativeBindingsContext()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				_context = new WglBindingsContext();
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				_context = new GlxBindingsContext();
			else
				throw new PlatformNotSupportedException();
		}

		public IntPtr GetProcAddress(string procName)
		{
			return _context.GetProcAddress(procName);
		}
	}
}