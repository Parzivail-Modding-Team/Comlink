namespace Nedry.Pin
{
	public class FlowOutputPin : IOutputPin
	{
		/// <inheritdoc />
		public string Name { get; set; }

		/// <inheritdoc />
		public PinId PinId { get; set; }

		/// <inheritdoc />
		public uint Color { get; set; } = 0xFF_FFFFFF;

		/// <inheritdoc />
		public FlowOutputPin(PinId pinId, string name)
		{
			PinId = pinId;
			Name = name;
		}

		/// <inheritdoc />
		public bool CanConnectTo(IPin other)
		{
			return other is FlowInputPin;
		}

		protected bool Equals(FlowOutputPin other)
		{
			return PinId.Equals(other.PinId);
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.GetType() == GetType() && Equals((FlowOutputPin) obj);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return PinId.GetHashCode();
		}

		public static bool operator ==(FlowOutputPin left, FlowOutputPin right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(FlowOutputPin left, FlowOutputPin right)
		{
			return !Equals(left, right);
		}
	}
}