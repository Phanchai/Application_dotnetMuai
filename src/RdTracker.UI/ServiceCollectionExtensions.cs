using Microsoft.Extensions.DependencyInjection;
using RdTracker.Core.Services;

namespace RdTracker.UI;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers everything the UI needs. Both hosts (MAUI and web) call this one method.</summary>
    public static IServiceCollection AddRdTracker(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IRdDataService>(sp =>
            new InMemoryRdDataService(DemoData.Create(sp.GetRequiredService<IClock>().Today)));
        services.AddSingleton<IAuthService, DemoAuthService>();
        services.AddSingleton<SessionState>();
        return services;
    }
}
