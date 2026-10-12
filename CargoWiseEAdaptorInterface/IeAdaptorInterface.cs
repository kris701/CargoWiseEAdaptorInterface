using CargoWiseEAdaptorInterface.Models;
using System.Xml.Linq;

namespace CargoWiseEAdaptorInterface
{
	public interface IeAdaptorInterface
	{
		public string InboundURL { get; set; }

		public Task<eAdaptorReponse> SendAsync(XElement content);
	}
}
