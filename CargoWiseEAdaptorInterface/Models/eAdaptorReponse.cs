using System.Xml.Linq;

namespace CargoWiseEAdaptorInterface.Models
{
	public class eAdaptorReponse
	{
		public bool HasFailed { get; set; } = false;
		public XElement? Body { get; set; }
		public string Log { get; set; } = "";
	}
}
