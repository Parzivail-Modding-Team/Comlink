using System;
using Nedry;

namespace Comlink.Render
{
	internal class FlowOutputPin : IOutputPin
	{
		/// <inheritdoc />
		public FlowOutputPin(string name)
		{
			Name = name;
			PinId = Guid.NewGuid();
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

		/// <inheritdoc />
		public string Name { get; set; }

		/// <inheritdoc />
		public Guid PinId { get; init; }

		/// <inheritdoc />
		public uint Color { get; set; } = 0xFF_FFFFFF;

		/// <inheritdoc />
		public bool CanConnectTo(IPin other)
		{
			return other is FlowInputPin;
		}
	}
}