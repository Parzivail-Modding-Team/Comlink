namespace Nedry.Pin
{
	public class TypeOutputPin : IOutputPin, ITypedPin
	{
		private string _type;

		/// <inheritdoc />
		public string Name { get; set; }

		/// <inheritdoc />
		public PinId PinId { get; init; }

		/// <inheritdoc />
		public uint Color { get; set; } = 0xFF_32cd32;

		/// <inheritdoc />
		public string Type
		{
			get => _type;
			set
			{
				_type = value;
				Color = HashColorConverter.GetColor(_type);
			}
		}

		public TypeOutputPin(PinId pinId, string name, string type)
		{
			PinId = pinId;
			Name = name;
			Type = type;
		}

		/// <inheritdoc />
		public bool CanConnectTo(IPin other)
		{
			return other is TypeInputPin tip && tip.Type == Type;
		}

		protected bool Equals(TypeOutputPin other)
		{
			return PinId.Equals(other.PinId);
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.GetType() == GetType() && Equals((TypeOutputPin) obj);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return PinId.GetHashCode();
		}

		public static bool operator ==(TypeOutputPin left, TypeOutputPin right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(TypeOutputPin left, TypeOutputPin right)
		{
			return !Equals(left, right);
		}
	}
}