using CargoWiseEAdaptorInterface.Models;
using System.Xml.Linq;

namespace CargoWiseEAdaptorInterface
{
	/// <summary>
	/// Simple event handler
	/// </summary>
	public delegate void eAdaptorInterfaceHandler();

	/// <summary>
	/// Main interface
	/// </summary>
	public interface IeAdaptorInterface
	{
		/// <summary>
		/// The inbound URL for the CargoWise instance
		/// </summary>
		public string InboundURL { get; set; }
		/// <summary>
		/// Timeout for requests. Set this to a rather high value, default is 10 minutes.
		/// </summary>
		public TimeSpan RequestTimeout { get; set; }
		/// <summary>
		/// Event that fires if the connection to the eAdaptor did not respond with a valid answer.
		/// This only fires for technical issues, not if the eAdaptor itself returns a "ERR" XML file back.
		/// </summary>
		public event eAdaptorInterfaceHandler? OnConnectionFailure;

		/// <summary>
		/// Send a request to the eAdaptor
		/// </summary>
		/// <param name="content"></param>
		/// <returns></returns>
		public Task<eAdaptorReponse> SendAsync(XElement content);
	}
}
