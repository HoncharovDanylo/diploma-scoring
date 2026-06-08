using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lending.Contracts.Events;

public sealed class IntegrationEnvelope
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; }

    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = "";

    [JsonPropertyName("occurredAtUtc")]
    public DateTimeOffset OccurredAtUtc { get; set; }

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = "";

    [JsonPropertyName("causationId")]
    public string? CausationId { get; set; }

    [JsonPropertyName("producer")]
    public string Producer { get; set; } = "";

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; }

    [JsonPropertyName("payload")]
    public JsonElement Payload { get; set; }

    public static IntegrationEnvelope Create<TPayload>(
        string eventType,
        string producer,
        int schemaVersion,
        TPayload payload,
        string correlationId,
        string? causationId = null)
    {
        return new IntegrationEnvelope
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            OccurredAtUtc = DateTimeOffset.UtcNow,
            CorrelationId = correlationId,
            CausationId = causationId,
            Producer = producer,
            SchemaVersion = schemaVersion,
            Payload = JsonSerializer.SerializeToDocument(payload, Json.Options).RootElement.Clone()
        };
    }

    public string SerializeToJson() => JsonSerializer.Serialize(this, Json.Options);

    public byte[] ToUtf8JsonBytes() => Encoding.UTF8.GetBytes(SerializeToJson());

    public static IntegrationEnvelope? Deserialize(string json)
    {
        return JsonSerializer.Deserialize<IntegrationEnvelope>(json, Json.Options);
    }

    public static JsonSerializerOptions GetSerializerOptions() => Json.Options;

    private static class Json
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }
}
