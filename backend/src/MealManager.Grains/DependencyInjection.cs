using MealManager.Application.Abstractions;
using MealManager.Grains.Projections;
using Microsoft.Extensions.DependencyInjection;

namespace MealManager.Grains;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Orleans-facing port implementations. The silo itself is configured by the host
    /// (UseOrleans in the Api; test factories co-host a silo with random ports).
    /// </summary>
    public static IServiceCollection AddBmmGrainServices(this IServiceCollection services)
    {
        services.AddScoped<ITotalsProjection, TotalsProjection>();
        services.AddScoped<ISummaryProvider, SummaryProvider>();
        return services;
    }
}
