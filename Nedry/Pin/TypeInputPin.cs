namespace Nedry.Pin
{
	public class TypeInputPin : IInputPin, ITypedPin
	{
		private string _type;

		/// <inheritdoc />
		public string Name { get; set; }

		/// <inheritdoc />
		public PinId PinId { get; set; }

		/// <inheritdoc />
		public uint Color { get; set; } = 0xFF_00bfff;

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

		public TypeInputPin(PinId pinId, string name, string type)
		{
			PinId = pinId;
			Name = name;
			Type = type;
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