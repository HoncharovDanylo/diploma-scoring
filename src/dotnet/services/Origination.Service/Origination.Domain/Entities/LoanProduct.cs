namespace Origination.Domain.Entities;

public sealed class LoanProduct
{
    public Guid LoanProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public decimal MinPrincipal { get; set; }
    public decimal MaxPrincipal { get; set; }
    public int MinTermDays { get; set; }
    public int MaxTermDays { get; set; }
    public decimal InterestRatePerDay { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
