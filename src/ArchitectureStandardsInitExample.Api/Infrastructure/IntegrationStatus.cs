using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ArchitectureStandardsInitExample.ServiceDefaults;

namespace ArchitectureStandardsInitExample.Api.Infrastructure;

/// <summary>
/// P8's visible half. Every optional integration this build knows about, whether
/// its configuration is present, and what is lost when it is not — reported by
/// <c>/health</c> and printed by the startup banner, so "which features are live
/// in this deployment?" is one request rather than an inspection of config.
/// </summary>
/// <param name="Name">Stable identifier, used as the health-check data key.</param>
/// <param name="Configured">Whether the integration's configuration is present.</param>
/// <param name="DegradesTo">What happens in its absence. Never "it breaks".</param>
public sealed record Integration(string Name, bool Configured, string DegradesTo);

public sealed class IntegrationStatus(IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;

    public IReadOnlyList<Integration> All =>
    [
        new("persistence",
            DatabaseProviderExtensions.ResolveProvider(_configuration, "apidb")
                != DatabaseProviderExtensions.DatabaseProvider.InMemory,
            "an in-memory database that is empty after every restart"),

        new("telemetry-export",
            !string.IsNullOrWhiteSpace(_configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]),
            "telemetry is collected and instrumented but exported nowhere"),

        new("azure-monitor",
            !string.IsNullOrWhiteSpace(_configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]),
            "no Azure Monitor export; OTLP export is unaffected"),
    ];

    public IReadOnlyList<string> Degraded =>
        [.. All.Where(integration => !integration.Configured).Select(integration => integration.Name)];

    /// <summary>
    /// Degraded is <see cref="HealthStatus.Healthy"/>, deliberately: a deployment
    /// running with reduced features is working as designed (P8). Reporting it as
    /// Degraded would take the machine out of rotation for a condition that is
    /// not a fault — the information belongs in the payload, not the status code.
    /// </summary>
    public HealthCheckResult ToHealthCheckResult()
    {
        var data = All.ToDictionary(
            integration => integration.Name,
            integration => (object)(integration.Configured
                ? "configured"
                : $"absent — {integration.DegradesTo}"));

        var degraded = Degraded;
        var description = degraded.Count == 0
            ? "Every optional integration is configured."
            : $"Running with reduced features: {string.Join(", ", degraded)}.";

        return HealthCheckResult.Healthy(description, data);
    }

    /// <summary>The startup banner prints exactly the list <c>/health</c> reports.</summary>
    public IEnumerable<string> BannerLines()
    {
        yield return "optional integrations:";
        foreach (var integration in All)
        {
            yield return integration.Configured
                ? $"  [ ok ] {integration.Name}"
                : $"  [ -- ] {integration.Name} — absent, degrades to {integration.DegradesTo}";
        }
    }
}
