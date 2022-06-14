using System;
using IntegrationTests.Extensions;

namespace IntegrationTests.Configuration
{
	public class Options1
	{
		#region Properties

		public virtual string Value1 { get; set; }

		#endregion

		#region Methods

		public override bool Equals(object obj)
		{
			return obj is Options1 options1 && string.Equals(this.Value1, options1.Value1, StringComparison.OrdinalIgnoreCase);
		}

		public override int GetHashCode()
		{
			return this.ToString().GetHashCode();
		}

		public override string ToString()
		{
			return $"{nameof(Options1)}: {nameof(this.Value1)} = {this.Value1.ToStringRepresentation()}";
		}

		#endregion
	}
}