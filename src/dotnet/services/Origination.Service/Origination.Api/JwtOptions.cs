namespace Origination.Api;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "Lending";
    public string Audience { get; set; } = "Lending";
}
