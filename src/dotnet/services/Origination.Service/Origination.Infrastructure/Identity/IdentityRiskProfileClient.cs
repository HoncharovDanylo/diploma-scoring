using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Origination.Application.Identity;

namespace Origination.Infrastructure.Identity;

public sealed class IdentityRiskProfileClient : IIdentityRiskProfileClient
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http;
    private readonly IdentityServiceOptions _options;

    public IdentityRiskProfileClient(HttpClient http, IOptions<IdentityServiceOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<IdentityRiskProfileSnapshot?> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl) || string.IsNullOrEmpty(_options.InternalApiKey))
            return null;

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"internal/v1/users/{userId:D}/risk-profile");
        request.Headers.TryAddWithoutValidation("X-Internal-Api-Key", _options.InternalApiKey);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<IdentityRiskProfileSnapshot>(Json, cancellationToken);
    }
}
