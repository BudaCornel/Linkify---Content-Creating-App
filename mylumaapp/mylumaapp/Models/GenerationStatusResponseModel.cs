using System.Text.Json.Serialization;

namespace MyLumaApp.Models
{
    public class GenerationStatusResponseModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("generation_type")]
        public string GenerationType { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("failure_reason")]
        public string FailureReason { get; set; }

        [JsonPropertyName("assets")]
        public GenerationAssets Assets { get; set; }
    }

    public class GenerationAssets
    {
        [JsonPropertyName("video")]
        public string Video { get; set; }

        [JsonPropertyName("image")]
        public string Image { get; set; }
    }
}
