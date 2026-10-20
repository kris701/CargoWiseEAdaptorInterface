using CargoWiseEAdaptorInterface.Helpers;
using CargoWiseEAdaptorInterface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SerializableHttps;
using SerializableHttps.AuthenticationMethods;
using SerializableHttps.Exceptions;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Xml.Linq;

namespace CargoWiseEAdaptorInterface
{
	/// <summary>
	/// Interface for eAdaptor Next
	/// </summary>
	public class eAdaptorNextInterface : IeAdaptorInterface
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
		/// <summary>
		/// Event that fires during auth if the certificate in use is soon to be expired.
		/// This will only fire once per restart
		/// </summary>
		public event eAdaptorInterfaceHandler? OnCertificateSoonExpires;
		/// <summary>
		/// Event that fires if the authentication with CargoWises OAuth fails
		/// </summary>
		public event eAdaptorInterfaceHandler? OnAuthFailure;

		private bool _hasFiredOnCertificateSoonExpires = false;

		private readonly string _oAuthURL;
		private readonly Guid _clientID;
		private readonly X509Certificate2 _clientCertificate;

		private DateTime _expiresTime = DateTime.MaxValue;
		private string? _oauthToken = null;
		private readonly SemaphoreSlim _mut = new SemaphoreSlim(1);
		private bool _serviceUnavailable = false;

		private readonly ILogger<eAdaptorNextInterface>? _logger;

		/// <summary>
		/// Main constructor
		/// </summary>
		/// <param name="config"></param>
		/// <param name="logger"></param>
		public eAdaptorNextInterface(eAdaptorNextConfig config, ILogger<eAdaptorNextInterface>? logger)
		{
			_logger = logger;
			InboundURL = config.InboundURL;
			_oAuthURL = config.CWOAuthURL;
			_clientID = config.CWClientID;
			_clientCertificate = config.CWCertificate;
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

			var httpsClient = new SerializableHttpsClient()
			{
				TimeOut = RequestTimeout
			};

			eAdaptorReponse response = new eAdaptorReponse();
			try
			{
				httpsClient.SetAuthentication(new JWTAuthenticationMethod(await GetAuthToken()));

				try
				{
					var body = await httpsClient.PostAsync<XElement, XElement>(content, InboundURL);
					response.Body = body;
					response.HasFailed = false;
					response.Log = ProcessLogHelpers.TryParseProcessLog(body);
				}
				catch (HttpGeneralException ex)
				{
					if (ex.ErrorCode == System.Net.HttpStatusCode.ServiceUnavailable)
					{
						_serviceUnavailable = true;
						_logger?.LogError($"Cargowise API is unavailable! Stopping all inbound for 1 minute.");
						return await RetryWithNewToken(content);
					}
					else
					{
						if (ex.Body.Contains(">ERR<"))
						{
							try
							{
								response.HasFailed = true;
								response.Body = XElement.Parse(ex.Body);
								response.Log = ProcessLogHelpers.TryParseProcessLog(response.Body);
							}
							catch (Exception ex3)
							{
								_logger?.LogError($"Could not deserialize output to XML: {ex3.Message}");
								response.Body = null;
								response.HasFailed = true;
								response.Log = ex3.Message;
							}
						}
						else
						{
							_logger?.LogError($"Error during inbound: {ex.Message}. Recreating token...");
							return await RetryWithNewToken(content);
						}
					}
				}
				catch (Exception ex)
				{
					_logger?.LogError($"Error during inbound: {ex.Message}. Recreating token...");
					return await RetryWithNewToken(content);
				}
			}
			catch (Exception ex)
			{
				_logger?.LogError($"Unknown error occured: {ex.Message}");
				OnConnectionFailure?.Invoke();
			}

			return response;
		}

		private async Task<eAdaptorReponse> RetryWithNewToken(XElement content)
		{
			if (_serviceUnavailable)
				await Task.Delay(TimeSpan.FromMinutes(1));

			await Task.Delay(1000);
			var httpsClient = new SerializableHttpsClient()
			{
				TimeOut = RequestTimeout
			};
			var response = new eAdaptorReponse();

			try
			{
				httpsClient.SetAuthentication(new JWTAuthenticationMethod(await GetAuthToken(true)));

				var body = await httpsClient.PostAsync<XElement, XElement>(content, InboundURL);
				response.Body = body;
				response.HasFailed = false;
				response.Log = ProcessLogHelpers.TryParseProcessLog(body);
			}
			catch (HttpGeneralException ex2)
			{
				_logger?.LogError($"Error during inbound: {ex2.Message}. Body: {ex2.Body}.");
				try
				{
					response.HasFailed = true;
					response.Body = XElement.Parse(ex2.Body);
					response.Log = ProcessLogHelpers.TryParseProcessLog(response.Body);
				}
				catch (Exception ex3)
				{
					_logger?.LogError($"Could not deserialize output to XML: {ex3.Message}");
					response.Body = null;
					response.HasFailed = true;
					response.Log = ex3.Message;
					OnConnectionFailure?.Invoke();
				}
			}
			catch (Exception ex2)
			{
				_logger?.LogError($"Error during inbound: {ex2.Message}. Giving up!");
				response.Body = null;
				response.HasFailed = true;
				response.Log = ex2.Message;
				OnConnectionFailure?.Invoke();
			}
			return response;
		}

		private async Task<string> GetAuthToken(bool force = false)
		{
			await _mut.WaitAsync();
			if (!force && _oauthToken != null && _expiresTime <= DateTime.UtcNow)
			{
				_mut.Release();
				return _oauthToken;
			}

			try
			{
				_expiresTime = DateTime.MaxValue;
				_oauthToken = null;

				if (_clientCertificate.NotAfter < DateTime.UtcNow.AddDays(30))
				{
					if (!_hasFiredOnCertificateSoonExpires)
					{
						_hasFiredOnCertificateSoonExpires = true;
						OnCertificateSoonExpires?.Invoke();
					}
				}

				var assertion = GetSignedClientAssertion();

				var nvc = new List<KeyValuePair<string, string>>
				{
					new("scope", $"{_clientID}/.default"),
					new("client_id", $"{_clientID}"),
					new("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
					new("client_assertion", assertion),
					new("grant_type", "client_credentials")
				};
				using var client = new HttpClient();
				using var req = new HttpRequestMessage(HttpMethod.Post, _oAuthURL) { Content = new FormUrlEncodedContent(nvc) };
				using var res = await client.SendAsync(req);
				if (res == null)
					throw new ArgumentNullException("res", "Auth response was null!");
				var content = await res.Content.ReadAsStringAsync();
				var token = JsonSerializer.Deserialize<JWTTokenModel>(content);

				if (token == null || token.AccessToken == null || token.ExpiresInSec == null)
					throw new ArgumentNullException("token", "Auth token was null!");

				_expiresTime = DateTime.UtcNow.AddSeconds((int)token.ExpiresInSec - 60);
				_oauthToken = token.AccessToken;

				_mut.Release();
				return _oauthToken;
			}
			catch (Exception ex)
			{
				_logger?.LogError($"Error during auth: {ex.Message}");
				_mut.Release();
				OnAuthFailure?.Invoke();
				throw new Exception($"Error during auth: {ex.Message}");
			}
		}

		private string GetSignedClientAssertion()
		{
			// no need to add exp, nbf as JsonWebTokenHandler will add them by default.
			var claims = new Dictionary<string, object>()
			{
				{ "aud", _oAuthURL },
				{ "iss", _clientID },
				{ "jti", Guid.NewGuid().ToString() },
				{ "sub", _clientID }
			};

			var securityTokenDescriptor = new SecurityTokenDescriptor
			{
				Claims = claims,
				SigningCredentials = new X509SigningCredentials(_clientCertificate)
			};

			var handler = new JsonWebTokenHandler();
			var signedClientAssertion = handler.CreateToken(securityTokenDescriptor);
			return signedClientAssertion;
		}
	}
}
