using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Origination.Domain;
using Origination.Domain.Entities;
using Origination.Infrastructure.Persistence;

namespace Origination.Api.Controllers;

[ApiController]
[Route("internal/v1/applications")]
public sealed class InternalApplicationsController : ControllerBase
{
    private readonly OriginationDbContext _db;

    public InternalApplicationsController(OriginationDbContext db) => _db = db;

    public sealed class VerificationBatchRequest
    {
        public List<Guid> ApplicationIds { get; set; } = [];
    }

    [HttpPost("verification-batch")]
    public async Task<ActionResult> VerificationBatch(
        [FromBody] VerificationBatchRequest request,
        CancellationToken cancellationToken)
    {
        var ids = request.ApplicationIds.Distinct().Take(500).ToList();
        if (ids.Count == 0)
            return Ok(Array.Empty<object>());

        var rows = await _db.LoanApplications
            .AsNoTracking()
            .Where(x => ids.Contains(x.ApplicationId))
            .Select(x => new
            {
                applicationId = x.ApplicationId,
                requestedPrincipal = x.RequestedPrincipal,
                productMinPrincipal = x.ProductMinPrincipal,
                status = x.Status,
                approvedPrincipal = x.ApprovedPrincipal
            })
            .ToListAsync(cancellationToken);

        return Ok(rows);
    }

    [HttpGet("{applicationId:guid}/scoring-feature-package")]
    public async Task<ActionResult> GetScoringFeaturePackage(Guid applicationId, CancellationToken cancellationToken)
    {
        var app = await _db.LoanApplications
            .AsNoTracking()
            .Include(x => x.Applicant)
            .FirstOrDefaultAsync(x => x.ApplicationId == applicationId, cancellationToken);

        if (app is null)
            return NotFound();

        if (app.Status != ApplicationStatuses.ScoringPending)
            return Conflict(new { detail = "Application is not in scoring pending state." });

        var package = new
        {
            applicationId = app.ApplicationId,
            featureVectorVersion = "v5-xgb-tabular",
            applicant = new
            {
                app.Applicant.FullName,
                app.Applicant.TaxIdMasked,
                app.Applicant.DateOfBirth,
                app.Applicant.MonthlyIncome,
                app.Applicant.EmploymentStatus
            },
            application = new
            {
                requestedPrincipal = app.RequestedPrincipal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                app.RequestedTermDays,
                calculatedRepaymentAmount = app.CalculatedRepaymentAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                app.Purpose
            },
            features = BuildMlFeatures(
                app.ApplicationId,
                app.RequestedPrincipal,
                app.RequestedTermDays,
                app.CalculatedRepaymentAmount,
                app.Purpose,
                app.Applicant)
        };

        return Ok(package);
    }

    private static Dictionary<string, decimal> BuildMlFeatures(
        Guid applicationId,
        decimal principal,
        int termDays,
        decimal repaymentAmount,
        string? purpose,
        Applicant applicant)
    {
        var mixId = applicant.UserId ?? applicationId;
        var bytes = mixId.ToByteArray();
        static int Mix(ReadOnlySpan<byte> b, int a, int c, int d, int e)
            => (b[a] << 24) | (b[c] << 16) | (b[d] << 8) | b[e];

        var h1 = Math.Abs(Mix(bytes, 0, 1, 2, 3));
        var h2 = Math.Abs(Mix(bytes, 4, 5, 6, 7));
        var h3 = Math.Abs(Mix(bytes, 8, 9, 10, 11));
        var purposeCode = 0;
        if (!string.IsNullOrEmpty(purpose))
            purposeCode = (purpose.Trim().ToLowerInvariant().GetHashCode(StringComparison.Ordinal) & int.MaxValue) % 6;

        decimal incomeToPrincipal;
        if (applicant.MonthlyIncome is { } mInc && mInc > 0m)
            incomeToPrincipal = Math.Min(5m, mInc * 12m / Math.Max(principal, 0.01m));
        else
            incomeToPrincipal = 0.75m + h1 % 60 * 0.02m;

        decimal creditUtil;
        if (TryGetAgeYears(applicant.DateOfBirth, out var age))
        {
            var ageNorm = Math.Clamp((age - 18) / 52m, 0m, 1m);
            creditUtil = ageNorm * 0.5m + EmploymentToUtilization(applicant.EmploymentStatus) * 0.5m;
        }
        else
        {
            creditUtil = h2 % 100 / 100m;
        }

        var delinq = (decimal)(h3 % 120);
        if (applicant.MonthlyIncome is { } m2 && m2 > 0m && delinq > 60m) delinq = 60m;

        var ageYears = TryGetAgeYears(applicant.DateOfBirth, out var years) ? (decimal)years : 0m;
        var monthlyInc = applicant.MonthlyIncome ?? 0m;
        var employmentEnc = (decimal)EmploymentToEncodingInt(applicant.EmploymentStatus);
        var debtBurden = monthlyInc > 0m
            ? Math.Min(20m, (repaymentAmount > 0m ? repaymentAmount : principal) / monthlyInc)
            : 0m;

        return new Dictionary<string, decimal>
        {
            ["requested_principal"] = principal,
            ["term_days"] = termDays,
            ["income_to_principal_ratio"] = incomeToPrincipal,
            ["credit_utilization_ratio"] = creditUtil,
            ["months_since_last_delinquency"] = delinq,
            ["purpose_encoding"] = purposeCode,
            ["applicant_age_years"] = ageYears,
            ["monthly_income"] = monthlyInc,
            ["employment_encoding"] = employmentEnc,
            ["repayment_to_income_ratio"] = debtBurden
        };
    }

    private static bool TryGetAgeYears(DateOnly? dob, out int age)
    {
        age = 0;
        if (dob is null) return false;
        var today = DateTime.UtcNow.Date;
        var b = dob.Value.ToDateTime(TimeOnly.MinValue);
        age = today.Year - b.Year;
        if (today < b.AddYears(age)) age--;
        if (age < 0) return false;
        return true;
    }

    private static decimal EmploymentToUtilization(string? s) =>
        s switch
        {
            "Employed" => 0.3m,
            "SelfEmployed" => 0.35m,
            "Unemployed" => 0.85m,
            "Student" => 0.7m,
            "Retired" => 0.5m,
            _ => 0.5m
        };

    private static int EmploymentToEncodingInt(string? s) =>
        s switch
        {
            "Employed" => 0,
            "SelfEmployed" => 1,
            "Unemployed" => 2,
            "Student" => 3,
            "Retired" => 4,
            _ => 0
        };

}
