using System;
using System.Collections;

namespace Nedry
{
	public class PinId
	{
		private readonly byte[] _id;
		public NodeId Node { get; }

		public PinId(string nodeId, string id)
		{
			Node = new NodeId(nodeId);
			_id = IdHelper.FromString(id);
		}

		public PinId(NodeId node, byte[] id)
		{
			Node = node;
			_id = id;
		}

		public static PinId NewId(NodeId parent, PinType type, short pinIdx)
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
			return HashCode.Combine(Node, _id);
		}

		public static bool operator ==(PinId left, PinId right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(PinId left, PinId right)
		{
			return !Equals(left, right);
		}
	}
}