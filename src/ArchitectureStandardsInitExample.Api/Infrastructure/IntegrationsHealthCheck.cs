using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArchitectureStandardsInitExample.Api.Infrastructure;

/// <summary>
/// Reports the same list the startup banner prints (P8). One source, two
/// surfaces — a health endpoint and a banner that disagree are worse than
/// neither, because one of them will be believed.
/// </summary>
public sealed class IntegrationsHealthCheck(IntegrationStatus integrations) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(integrations.ToHealthCheckResult());
}
