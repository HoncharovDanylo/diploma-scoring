using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Portfolio.Infrastructure;

public sealed class OriginationVerificationRow
{
    [JsonPropertyName("applicationId")]
    public Guid ApplicationId { get; set; }

    [JsonPropertyName("requestedPrincipal")]
    public decimal RequestedPrincipal { get; set; }

    [JsonPropertyName("productMinPrincipal")]
    public decimal ProductMinPrincipal { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("approvedPrincipal")]
    public decimal? ApprovedPrincipal { get; set; }
}

public sealed class OriginationReadClient(HttpClient http)
{
    public async Task<JsonDocument> GetPortfolioCandidatesAsync(DateOnly businessDate, CancellationToken cancellationToken)
    {
        var url = $"internal/v1/portfolio/candidates?businessDate={businessDate:yyyy-MM-dd}";
        var doc = await http.GetFromJsonAsync<JsonDocument>(url, cancellationToken);
        return doc ?? JsonDocument.Parse("{}");
    }

    public async Task<decimal> GetIssuedPrincipalAsync(DateOnly businessDate, CancellationToken cancellationToken)
    {
        var url = $"internal/v1/portfolio/issued-summary?businessDate={businessDate:yyyy-MM-dd}";
        var doc = await http.GetFromJsonAsync<JsonDocument>(url, cancellationToken);
        if (doc is null || !doc.RootElement.TryGetProperty("issuedPrincipal", out var val))
            return 0m;

        if (val.ValueKind == JsonValueKind.Number && val.TryGetDecimal(out var numeric))
            return numeric;

        if (val.ValueKind == JsonValueKind.String &&
            decimal.TryParse(val.GetString(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        return 0m;
    }

    public async Task<IReadOnlyDictionary<Guid, OriginationVerificationRow>> GetVerificationBatchAsync(
        IReadOnlyList<Guid> applicationIds,
        CancellationToken cancellationToken)
    {
        if (applicationIds.Count == 0)
            return new Dictionary<Guid, OriginationVerificationRow>();

        var url = "internal/v1/applications/verification-batch";
        using var response = await http.PostAsJsonAsync(
            url,
            new { applicationIds },
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var rows = await response.Content.ReadFromJsonAsync<List<OriginationVerificationRow>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);
        if (rows is null)
            return new Dictionary<Guid, OriginationVerificationRow>();

        return rows.ToDictionary(x => x.ApplicationId, x => x);
    }
}
