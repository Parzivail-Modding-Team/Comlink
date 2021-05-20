using System.IO;
using System.Windows;

namespace Comlink
{
	public class ClipboardHelper
	{
		public static void PutBytesOnClipboard(string format, MemoryStream ms)
		{
			var data = new DataObject();
			data.SetData(format, ms, false);
			Clipboard.SetDataObject(data, true);
		}

		public static MemoryStream GetBytesFromClipboard(string format)
		{
			if (Clipboard.GetDataObject() is not DataObject retrievedData || !retrievedData.GetDataPresent(format) || retrievedData.GetData(format) is not MemoryStream memoryStream)
				return null;

			return memoryStream;
		}
	}
}