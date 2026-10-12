using System.Text.Json.Serialization;

namespace CargoWiseEAdaptorInterface.Models
{
	internal class JWTTokenModel
	{
		[JsonPropertyName("access_token")]
		public string? AccessToken { get; set; }
		[JsonPropertyName("expires_in")]
		public int? ExpiresInSec { get; set; }
	}
}
