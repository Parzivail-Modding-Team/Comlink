namespace Comlink.Controls
{
	public partial class SingleTypeNodeAddDialog
	{
		public string ValueTitle { get; set; }
		public string Value { get; set; }
		public string Type { get; set; }

		public SingleTypeNodeAddDialog(string title, string value, string type)
		{
			ValueTitle = title;
			Value = value;
			Type = type;

			InitializeComponent();
		}
	}
}