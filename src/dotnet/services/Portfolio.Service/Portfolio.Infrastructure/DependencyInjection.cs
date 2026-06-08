using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application;

namespace Portfolio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPortfolioInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddDbContext<PortfolioDbContext>(o =>
            o.UseSqlServer(configuration.GetConnectionString("Portfolio")));

        services.AddHttpClient<OriginationReadClient>((sp, client) =>
        {
            var baseUrl = configuration["Origination:BaseUrl"] ?? "http://localhost:5002";
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            var key = configuration["Origination:InternalApiKey"] ?? "";
            client.DefaultRequestHeaders.Remove("X-Internal-Api-Key");
            client.DefaultRequestHeaders.Add("X-Internal-Api-Key", key);
        });

        services.AddScoped<IPortfolioRunsService, PortfolioRunService>();
        services.AddScoped<PortfolioOptimizationReplyHandler>();
        services.AddSingleton<RabbitMqConnectionHolder>();
        services.AddHostedService<PortfolioOutboxRelayHostedService>();
        services.AddHostedService<PortfolioOptimizationConsumerHostedService>();

        return services;
    }
}
