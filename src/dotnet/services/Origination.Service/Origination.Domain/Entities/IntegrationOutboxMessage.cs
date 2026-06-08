namespace Origination.Domain.Entities;

public class IntegrationOutboxMessage
{
    public long OutboxId { get; set; }
    public string EventType { get; set; } = "";
    public string EnvelopeJson { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}
