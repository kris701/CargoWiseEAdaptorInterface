using System.Xml.Linq;

namespace CargoWiseEAdaptorInterface.Models
{
	/// <summary>
	/// A response from the eAdaptor
	/// </summary>
	public class eAdaptorReponse
	{
		/// <summary>
		/// A value defining if the request failed.
		/// This only gets set to true if the eAdaptor DIRECTLY gives an error, not if a requested item was not found
		/// </summary>
		public bool HasFailed { get; set; } = false;
		/// <summary>
		/// The full XML body
		/// </summary>
		public XElement? Body { get; set; }
		/// <summary>
		/// The log from the XML body
		/// </summary>
		public string Log { get; set; } = "";
	}
}
