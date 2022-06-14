using System;
using IntegrationTests.Extensions;

namespace IntegrationTests
{
	public class TriggeredChange
	{
		#region Properties

		/// <summary>
		/// If the options really are changed or not.
		/// </summary>
		public virtual bool Changed { get; set; }

		public virtual string ConfigurationName { get; set; }

		/// <summary>
		/// The label for the listener, from code, eg. "onChange1".
		/// </summary>
		public virtual string Label { get; set; }

		/// <summary>
		/// The code for the listener, eg. "options1Monitor.OnChange((options1) =>".
		/// </summary>
		public virtual string Listener { get; set; }

		public virtual string OptionsAsString { get; set; }
		public virtual DateTimeOffset Timestamp { get; set; }

		#endregion

		#region Methods

		public override string ToString()
		{
			return $"{this.Label}: {nameof(this.Changed)} = {this.Changed}, {nameof(this.ConfigurationName)} = {this.ConfigurationName.ToStringRepresentation()}, {nameof(this.Listener)} = {this.Listener.ToStringRepresentation()}, {nameof(this.OptionsAsString)} = {this.OptionsAsString.ToStringRepresentation()}, {nameof(this.Timestamp)} = {this.Timestamp.Ticks}";
		}

		#endregion
	}
}