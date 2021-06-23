using System.IO;
using System.Reflection;

namespace Hyperwave.Resources
{
	public static class ResourceHelper
	{
		public static Stream GetResource(string filename)
		{
			var assembly = Assembly.GetExecutingAssembly();
			return assembly.GetManifestResourceStream("Hyperwave.Resources." + filename);
		}
	}
}