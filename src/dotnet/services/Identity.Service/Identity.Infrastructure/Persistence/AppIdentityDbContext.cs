using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

public sealed class AppIdentityDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<AppUser>(e =>
        {
            e.Property(x => x.MonthlyIncome).HasPrecision(18, 2);
            e.Property(x => x.EmploymentStatus).HasMaxLength(64);
            e.Property(x => x.RegionCode).HasMaxLength(16);
            e.Property(x => x.TaxIdMasked).HasMaxLength(32);
        });
    }
}
