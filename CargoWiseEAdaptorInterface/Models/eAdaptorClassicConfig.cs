namespace CargoWiseEAdaptorInterface.Models
{
	/// <summary>
	/// Config for the Classic eAdaptor
	/// </summary>
	public class eAdaptorClassicConfig : IeAdaptorConfig
	{
		/// <summary>
		/// Username for the eAdaptor
		/// </summary>
		public string Username { get; set; }
		/// <summary>
		/// Password for the eAdaptor
		/// </summary>
		public string Password { get; set; }

		/// <summary>
		/// Main constructor
		/// </summary>
		/// <param name="inboundURL"></param>
		/// <param name="username"></param>
		/// <param name="password"></param>
		public eAdaptorClassicConfig(string inboundURL, string username, string password) : base(inboundURL)
		{
			Username = username;
			Password = password;
		}
	}
}
