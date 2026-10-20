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
		/// <summary>
		/// Event that fires if the connection to the eAdaptor did not respond with a valid answer.
		/// This only fires for technical issues, not if the eAdaptor itself returns a "ERR" XML file back.
		/// </summary>
		public event eAdaptorInterfaceHandler? OnConnectionFailure;

		private readonly string _username;
		private readonly string _password;
		private bool _serviceUnavailable = false;

		private readonly ILogger<eAdaptorClassicInterface>? _logger;

		/// <summary>
		/// Main constructor
		/// </summary>
		/// <param name="config"></param>
		/// <param name="logger"></param>
		public eAdaptorClassicInterface(eAdaptorClassicConfig config, ILogger<eAdaptorClassicInterface>? logger)
		{
			InboundURL = config.InboundURL;
			_username = config.Username;
			_password = config.Password;
			_logger = logger;
		}

		/// <summary>
		/// Send a request to the eAdaptor
		/// </summary>
		/// <param name="content"></param>
		/// <returns></returns>
		public async Task<eAdaptorReponse> SendAsync(XElement content)
		{
			if (_serviceUnavailable)
			{
				await Task.Delay(TimeSpan.FromMinutes(1));
				_serviceUnavailable = false;
			}

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
				if (ex2.ErrorCode == System.Net.HttpStatusCode.ServiceUnavailable)
				{
					_serviceUnavailable = true;
					_logger?.LogError($"Cargowise API is unavailable! Stopping all inbound for 1 minute.");
					response.Body = null;
					response.HasFailed = true;
				}
				else
				{
					try
					{
						var body = XElement.Parse(ex2.Body);
						response.Body = body;
						response.HasFailed = true;
						response.Log = ProcessLogHelpers.TryParseProcessLog(body);
					}
					catch (Exception ex3)
					{
						_logger?.LogError($"Could not deserialize output to XML: {ex3.Message}");
						response.Body = null;
						response.HasFailed = true;
						response.Log = ex3.Message;
					}
				}
			}
			catch (Exception ex)
			{
				_logger?.LogError("Error during inbound: " + ex.Message);
				response.Body = null;
				response.HasFailed = true;
				response.Log = ex.Message;

				OnConnectionFailure?.Invoke();
			}

			return response;
		}
	}
}
