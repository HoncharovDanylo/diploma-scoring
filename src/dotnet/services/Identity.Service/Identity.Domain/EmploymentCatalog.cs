namespace Identity.Domain;

public static class EmploymentCatalog
{
    public const string Employed = "Employed";
    public const string SelfEmployed = "SelfEmployed";
    public const string Unemployed = "Unemployed";
    public const string Student = "Student";
    public const string Retired = "Retired";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Employed, SelfEmployed, Unemployed, Student, Retired
    };
}
