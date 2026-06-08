using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Origination.Application;
using Origination.Application.Identity;
using Origination.Infrastructure.Identity;
using Origination.Infrastructure.Messaging;
using Origination.Infrastructure.Persistence;

namespace Origination.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOriginationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<IdentityServiceOptions>(configuration.GetSection(IdentityServiceOptions.SectionName));
        services.Configure<LoanTermsOptions>(configuration.GetSection(LoanTermsOptions.SectionName));
        services.AddHttpClient<IIdentityRiskProfileClient, IdentityRiskProfileClient>((sp, client) =>
        {
            var o = sp.GetRequiredService<IOptions<IdentityServiceOptions>>().Value;
            var baseUrl = o.BaseUrl.Trim().TrimEnd('/');
            if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
                client.BaseAddress = uri;
        });
        services.AddDbContext<OriginationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Origination")));

        services.AddScoped<ILoanOriginationService, LoanOriginationService>();
        services.AddScoped<ScoringReplyHandler>();
        services.AddSingleton<RabbitMqConnectionHolder>();
        services.AddHostedService<OutboxRelayHostedService>();
        services.AddHostedService<ScoringReplyConsumerHostedService>();

        return services;
    }
}
