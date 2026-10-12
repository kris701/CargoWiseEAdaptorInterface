using CargoWiseEAdaptorInterface.Helpers;
using CargoWiseEAdaptorInterface.Models;
using Microsoft.Extensions.Logging;
using SerializableHttps;
using SerializableHttps.AuthenticationMethods;
using SerializableHttps.Exceptions;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;

namespace CargoWiseEAdaptorInterface
{
	/// <summary>
	/// Interface for eAdaptor Classic
	/// </summary>
	public class eAdaptorClassicInterface : IeAdaptorInterface
	{
		/// <summary>
		/// The inbound URL for the CargoWise instance
		/// </summary>
		public string InboundURL { get; set; }
		/// <summary>
		/// Timeout for requests. Set this to a rather high value, default is 10 minutes.
		/// </summary>
		public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromMinutes(10);

		private readonly string _username;
		private readonly string _password;

		private readonly ILogger<eAdaptorClassicInterface> _logger;

		/// <summary>
		/// Main constructor
		/// </summary>
		/// <param name="url"></param>
		/// <param name="username"></param>
		/// <param name="password"></param>
		/// <param name="logger"></param>
		public eAdaptorClassicInterface(string url, string username, string password, ILogger<eAdaptorClassicInterface> logger)
		{
			InboundURL = url;
			_username = username;
			_password = password;
			_logger = logger;
		}

		/// <summary>
		/// Send a request to the eAdaptor
		/// </summary>
		/// <param name="content"></param>
		/// <returns></returns>
		public async Task<eAdaptorReponse> SendAsync(XElement content)
		{
			var httpsClient = new SerializableHttpsClient();
			httpsClient.SetAuthentication(new ManualAuthenticationMethod(
				new AuthenticationHeaderValue(
				"Basic",
				Convert.ToBase64String(Encoding.GetEncoding("iso-8859-1").GetBytes(string.Format("{0}:{1}",
				_username,
				_password
				))))));
			httpsClient.TimeOut = RequestTimeout;
			eAdaptorReponse response = new eAdaptorReponse();

			try
			{
				var body = await httpsClient.PostAsync<XElement, XElement>(content, InboundURL);
				response.Body = body;
				response.HasFailed = false;
				response.Log = ProcessLogHelpers.TryParseProcessLog(body);
			}
			catch (HttpGeneralException ex2)
			{
				_logger.LogError($"Error during inbound: {ex2.Message}. Body: {ex2.Body}.");
				try
				{
					var body = XElement.Parse(ex2.Body);
					response.Body = body;
					response.HasFailed = true;
					response.Log = ProcessLogHelpers.TryParseProcessLog(body);
				}
				catch (Exception ex3)
				{
					_logger.LogError($"Could not reserialize output to XML: {ex3.Message}");
					response.Body = null;
					response.HasFailed = true;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError("Error during inbound: " + ex.Message);
				response.Body = null;
				response.HasFailed = true;
			}

			return response;
		}
	}
}
