using System;
using IntegrationTests.Extensions;

namespace IntegrationTests.Configuration
{
	public class Options2
	{
		#region Properties

		public virtual string Value1 { get; set; }
		public virtual string Value2 { get; set; }

		#endregion

		#region Methods

		public override bool Equals(object obj)
		{
			return obj is Options2 options2 && string.Equals(this.Value1, options2.Value1, StringComparison.OrdinalIgnoreCase) && string.Equals(this.Value2, options2.Value2, StringComparison.OrdinalIgnoreCase);
		}

		public override int GetHashCode()
		{
			return this.ToString().GetHashCode();
		}

		public override string ToString()
		{
			return $"{nameof(Options2)}: {nameof(this.Value1)} = {this.Value1.ToStringRepresentation()}, {nameof(this.Value2)} = {this.Value2.ToStringRepresentation()}";
		}

		#endregion
	}
}