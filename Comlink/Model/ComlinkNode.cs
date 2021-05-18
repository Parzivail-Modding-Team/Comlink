using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Nedry;
using UniqueId = Nedry.UniqueId;

namespace Comlink.Model
{
	public class ComlinkNode : Node
	{
		public NodeType NodeType { get; init; }

		/// <inheritdoc />
		public ComlinkNode(NodeType type, UniqueId nodeId) : base(nodeId)
		{
			NodeType = type;
		}

		public string Serialize()
		{
			// return JsonConvert.SerializeObject(this);

			using var sww = new StringWriter();
			using var writer = XmlWriter.Create(sww);

			new XmlSerializer(GetType()).Serialize(writer, this);
			var xml = sww.ToString(); // Your XML

			return xml;
		}

		public static ComlinkNode Deserialize()
		{
			return null;
		}
	}
}