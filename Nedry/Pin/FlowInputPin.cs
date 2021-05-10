namespace Nedry.Pin
{
	public class FlowInputPin : IInputPin
	{
		/// <inheritdoc />
		public string Name { get; set; } = string.Empty;

		/// <inheritdoc />
		public PinId PinId { get; init; }

		/// <inheritdoc />
		public uint Color { get; set; } = 0xFF_FFFFFF;

		/// <inheritdoc />
		public FlowInputPin(PinId pinId)
		{
			PinId = pinId;
		}

		/// <inheritdoc />
		public bool CanConnectTo(IPin other)
		{
			return other is FlowOutputPin;
		}

		protected bool Equals(FlowInputPin other)
		{
			return PinId.Equals(other.PinId);
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.GetType() == GetType() && Equals((FlowInputPin) obj);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return PinId.GetHashCode();
		}

		public static bool operator ==(FlowInputPin left, FlowInputPin right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(FlowInputPin left, FlowInputPin right)
		{
			return !Equals(left, right);
		}
	}
}