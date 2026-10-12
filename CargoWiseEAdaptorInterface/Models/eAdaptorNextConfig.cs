using System.Security.Cryptography.X509Certificates;

namespace CargoWiseEAdaptorInterface.Models
{
	/// <summary>
	/// Config for the Next eAdaptor
	/// </summary>
	public class eAdaptorNextConfig : IeAdaptorConfig
	{
		/// <summary>
		/// CargoWise OAuth URL
		/// </summary>
		public string CWOAuthURL { get; set; }
		/// <summary>
		/// CargoWise Client ID
		/// </summary>
		public Guid CWClientID { get; set; }
		/// <summary>
		/// Certificate from CargoWise
		/// </summary>
		public X509Certificate2 CWCertificate { get; set; }

		/// <summary>
		/// Main constructor
		/// </summary>
		/// <param name="inboundURL"></param>
		/// <param name="cWOAuthURL"></param>
		/// <param name="cWClientID"></param>
		/// <param name="cWCertificate"></param>
		public eAdaptorNextConfig(string inboundURL, string cWOAuthURL, Guid cWClientID, X509Certificate2 cWCertificate) : base(inboundURL)
		{
			CWOAuthURL = cWOAuthURL;
			CWClientID = cWClientID;
			CWCertificate = cWCertificate;
		}
	}
}
