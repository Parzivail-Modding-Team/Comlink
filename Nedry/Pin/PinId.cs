using System.Collections;
using System.Linq;

namespace Nedry.Pin
{
	public class PinId
	{
		private readonly byte[] _id;
		public UniqueId Node { get; }

		public PinId(string nodeId, string id)
		{
			Node = new UniqueId(nodeId);
			_id = IdHelper.FromString(id);
		}

		public PinId(UniqueId node, byte[] id)
		{
			Node = node;
			_id = id;
		}

		public static PinId NewId(UniqueId parent, PinType type, short pinIdx)
		{
			return new(parent, new[] {(byte) type, (byte) ((pinIdx >> 8) & 0xFF), (byte) (pinIdx & 0xFF)});
		}

		protected bool Equals(PinId other)
		{
			return Node.Equals(other.Node) &&
			       ((IStructuralEquatable) _id).Equals(other._id, StructuralComparisons.StructuralEqualityComparer);
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.GetType() == GetType() && Equals((PinId) obj);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				const int p = 16777619;
				var hash = _id.Aggregate((int) 2166136261, (current, t) => (current ^ t) * p);
				hash += hash << 13;
				hash ^= hash >> 7;
				hash += hash << 3;
				hash ^= hash >> 17;
				hash += hash << 5;
				return hash ^ (31 * Node.GetHashCode());
			}
		}

		public static bool operator ==(PinId left, PinId right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(PinId left, PinId right)
		{
			return !Equals(left, right);
		}

		public byte[] GetPinBytes()
		{
			return _id;
		}
	}
}