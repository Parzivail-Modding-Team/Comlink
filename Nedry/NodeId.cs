using System.Collections;

namespace Nedry
{
	public class NodeId
	{
		private readonly byte[] _id;

		public NodeId(string id)
		{
			_id = IdHelper.FromString(id);
		}

		public NodeId(byte[] id)
		{
			_id = id;
		}

		public static NodeId NewId()
		{
			return new(IdHelper.RandomBytes(8));
		}

		protected bool Equals(NodeId other)
		{
			return ((IStructuralEquatable) _id).Equals(other._id, StructuralComparisons.StructuralEqualityComparer);
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.GetType() == GetType() && Equals((NodeId) obj);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return _id.GetHashCode();
		}

		public static bool operator ==(NodeId left, NodeId right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(NodeId left, NodeId right)
		{
			return !Equals(left, right);
		}
	}
}