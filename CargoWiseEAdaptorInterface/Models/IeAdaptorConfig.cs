namespace CargoWiseEAdaptorInterface.Models
{
	/// <summary>
	/// Base config for the interfaces
	/// </summary>
	public abstract class IeAdaptorConfig
	{
		/// <summary>
		/// The inbound URL for the CargoWise instance
		/// </summary>
		public string InboundURL { get; set; }

		/// <summary>
		/// Main constructor
		/// </summary>
		/// <param name="inboundURL"></param>
		protected IeAdaptorConfig(string inboundURL)
		{
			InboundURL = inboundURL;
		}
	}
}
