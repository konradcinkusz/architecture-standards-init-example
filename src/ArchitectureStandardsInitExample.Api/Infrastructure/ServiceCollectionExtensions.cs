using ArchitectureStandardsInitExample.Api.Diagnostics;
using ArchitectureStandardsInitExample.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArchitectureStandardsInitExample.Api.Infrastructure;

/// <summary>
/// P9 — the wiring lives here so <c>Program.cs</c> reads as a list of
/// capabilities rather than as configuration code.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Persistence this service owns, and the schema work that keeps it current (P3, P4).</summary>
    public static IServiceCollection AddApiPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // The in-memory database name is a configuration seam, so each test class
        // gets its own and the suite survives parallelization (TESTING-STRATEGY §5).
        // The seam is built into the service rather than faked in the tests.
        var inMemoryDatabaseName = configuration["InMemoryDatabaseName"] ?? "apidb-inmemory";
        services.AddDatabaseContext<ApiDbContext>(configuration, "apidb", inMemoryDatabaseName);
        services.AddSingleton<MigrationCompletionSignal>();
        services.AddHostedService<MigrationHostedService>();
        return services;
    }

    /// <summary>
    /// P8 — the state of every optional integration, surfaced through the health
    /// endpoint and the startup banner from the same source, so the two cannot
    /// disagree.
    /// </summary>
    public static IServiceCollection AddIntegrationReporting(this IServiceCollection services)
    {
        services.AddSingleton<IntegrationStatus>();

        services.AddHealthChecks().AddCheck<IntegrationsHealthCheck>(
            "integrations",
            tags: ["ready"]);

        return services;
    }

    /// <summary>The one vertical slice this template ships.</summary>
    public static IServiceCollection AddDiagnosticsSlice(this IServiceCollection services)
    {
        services.AddHostedService<BootRecorder>();
        return services;
    }
}
