using System;
using Nedry;

namespace Comlink.Model
{
	internal class TypeInputPin : IInputPin
	{
		public Type Type { get; set; }

		/// <inheritdoc />
		public string Name { get; set; }

		/// <inheritdoc />
		public Guid PinId { get; init; }

		/// <inheritdoc />
		public uint Color { get; set; } = 0xFF_00bfff;

		public TypeInputPin(Guid pinId, string name, Type type)
		{
			PinId = pinId;
			Name = name;
			Type = type;
			PinId = Guid.NewGuid();

			// TODO
			Color = (uint) ((type.GetHashCode() & 0xFFFFFF) | 0xFF000000);
		}

		/// <inheritdoc />
		public bool CanConnectTo(IPin other)
		{
			return other is TypeOutputPin bop && bop.Type == Type;
		}

		public static bool operator ==(TypeInputPin left, TypeInputPin right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(TypeInputPin left, TypeInputPin right)
		{
			return !Equals(left, right);
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.GetType() == GetType() && Equals((TypeInputPin) obj);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return PinId.GetHashCode();
		}

		protected bool Equals(TypeInputPin other)
		{
			return PinId.Equals(other.PinId);
		}
	}
}