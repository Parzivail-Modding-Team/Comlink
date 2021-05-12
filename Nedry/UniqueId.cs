using System.Collections;
using System.Linq;

namespace Nedry
{
	public class UniqueId
	{
		private readonly byte[] _id;

		public UniqueId(string id)
		{
			_id = IdHelper.FromString(id);
		}

		public UniqueId(byte[] id)
		{
			_id = id;
		}

		public static UniqueId NewId()
		{
			return new(IdHelper.RandomBytes(8));
		}

		protected bool Equals(UniqueId other)
		{
			return ((IStructuralEquatable) _id).Equals(other._id, StructuralComparisons.StructuralEqualityComparer);
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.GetType() == GetType() && Equals((UniqueId) obj);
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
				return hash;
			}
		}

		public static bool operator ==(UniqueId left, UniqueId right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(UniqueId left, UniqueId right)
		{
			return !Equals(left, right);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"UniqueId<{IdHelper.ToString(_id)}>";
		}
	}
}