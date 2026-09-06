using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ArchitectureStandardsInitExample.Api.Tests.Infrastructure;

/// <summary>
/// Hosts the real service with <em>no</em> configuration at all — no connection
/// string, no OTLP endpoint, no cloud credential of any kind. That is P8's
/// literal test: a fresh clone with zero credentials must produce a working
/// system with reduced features, and this factory is how CI checks it on every
/// run rather than a human checking it once.
/// <para>
/// Isolation is by constructor (TESTING-STRATEGY §5): each instance names its own
/// in-memory database, so no test sees another's rows and the suite survives
/// parallelization.
/// </para>
/// </summary>
public sealed class ZeroCredentialFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"apidb-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            // Start from nothing, then add back only the in-memory database name,
            // so a developer's own user-secrets cannot make this test pass on
            // their machine and fail in CI.
            configuration.Sources.Clear();
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:apidb"] = null,
                ["DATABASE_PROVIDER"] = null,
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = null,
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = null,
                ["InMemoryDatabaseName"] = _databaseName
            });
        });
    }
}
