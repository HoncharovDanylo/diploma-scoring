namespace Origination.Infrastructure.Identity;

public sealed class IdentityServiceOptions
{
    public const string SectionName = "Identity";
    public string BaseUrl { get; set; } = "http://localhost:5001";
    public string InternalApiKey { get; set; } = "";
}
