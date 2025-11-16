using System.Text.Json.Serialization;

namespace BlazorExecutionFlow.Models.DTOs
{
    public class DfConnectionCreatedEventPayload
    {
        [JsonPropertyName("output_id")]
        public required string OutputId { get; set; }

        [JsonPropertyName("input_id")]
        public required string InputId { get; set; }

        [JsonPropertyName("output_class")]
        public required string OutputClass { get; set; }

        [JsonPropertyName("input_class")]
        public required string InputClass { get; set; }
    }
}
