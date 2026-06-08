namespace Origination.Application.Identity;

public interface IIdentityRiskProfileClient
{
    Task<IdentityRiskProfileSnapshot?> GetAsync(Guid userId, CancellationToken cancellationToken);
}
