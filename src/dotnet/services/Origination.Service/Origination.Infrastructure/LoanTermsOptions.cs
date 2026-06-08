namespace Origination.Infrastructure;

public sealed class LoanTermsOptions
{
    public const string SectionName = "LoanTerms";
    public int MaxTermDays { get; set; } = 365;
}
