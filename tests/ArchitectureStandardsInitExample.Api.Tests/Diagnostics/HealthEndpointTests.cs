using System.Net;
using System.Text.Json;
using ArchitectureStandardsInitExample.Api.Tests.Infrastructure;

namespace ArchitectureStandardsInitExample.Api.Tests.Diagnostics;

/// <summary>
/// The health endpoint's degraded-integration report (P8). This is the first
/// test deliberately: it is the one that fails the day somebody registers an
/// optional integration unconditionally, which is the failure that makes a fresh
/// clone unrunnable and is otherwise found by a new contributor on their first
/// morning.
/// </summary>
public sealed class HealthEndpointTests : IClassFixture<ZeroCredentialFactory>
{
    private readonly ZeroCredentialFactory _factory;

    public HealthEndpointTests(ZeroCredentialFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_answers_200_with_no_credentials_configured()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_names_every_optional_integration_and_what_it_degrades_to()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(body);

        var integrations = document.RootElement
            .GetProperty("checks")
            .EnumerateArray()
            .Single(check => check.GetProperty("name").GetString() == "integrations");

        var data = integrations.GetProperty("data");

        // With nothing configured, all three optional integrations report absent,
        // and each says what is lost — not merely that it is missing.
        foreach (var name in new[] { "persistence", "telemetry-export", "azure-monitor" })
        {
            var value = data.GetProperty(name).GetString();
            Assert.NotNull(value);
            Assert.StartsWith("absent", value);
        }

        // The degrade text is the part that saves an afternoon: it says what you
        // lose, not merely that something is unset.
        Assert.Contains("in-memory", data.GetProperty("persistence").GetString()!);
        Assert.Contains("exported nowhere", data.GetProperty("telemetry-export").GetString()!);

        // Degraded is still Healthy: a deployment running with reduced features
        // is working as designed, and reporting it unhealthy would take the
        // machine out of rotation for a condition that is not a fault.
        Assert.Equal("Healthy", document.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Alive_answers_200_and_is_not_the_readiness_report()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/alive", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // /alive carries only the liveness-tagged checks, so it must not carry the
        // integration report. A liveness probe that fails when an optional
        // integration is absent kills a machine that is working correctly.
        Assert.DoesNotContain("integrations", body, StringComparison.OrdinalIgnoreCase);
    }
}
