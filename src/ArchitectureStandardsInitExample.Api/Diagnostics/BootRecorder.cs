using System.Reflection;
using ArchitectureStandardsInitExample.Api.Infrastructure;
using ArchitectureStandardsInitExample.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ArchitectureStandardsInitExample.Api.Diagnostics;

/// <summary>
/// Writes one <see cref="BootRecord"/> per start, after the schema is ready.
/// <para>
/// It awaits <see cref="MigrationCompletionSignal"/> first — this is the
/// corollary in SERVICE-API-PATTERNS §7 made concrete: every hosted service other
/// than the migration itself waits for it, or it races the schema and dies on a
/// missing table.
/// </para>
/// </summary>
public sealed class BootRecorder(
    IServiceScopeFactory scopeFactory,
    MigrationCompletionSignal migrations,
    IntegrationStatus integrations,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<BootRecorder> logger) : BackgroundService
{
    public static string Version =>
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            .Split('+')[0]
        ?? "0.0.0";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The banner prints before the write, so it is visible even if the
        // database is unreachable — the moment you most want to read it.
        if (logger.IsEnabled(LogLevel.Information))
        {
            foreach (var line in integrations.BannerLines())
            {
                logger.LogInformation("{BannerLine}", line);
            }
        }

        try
        {
            await migrations.WaitAsync(stoppingToken);

            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApiDbContext>();

            var record = new BootRecord
            {
                Version = Version,
                Environment = environment.EnvironmentName,
                DatabaseProvider = DatabaseProviderExtensions.ResolveProvider(configuration, "apidb").ToString(),
                DegradedIntegrations = string.Join(",", integrations.Degraded)
            };

            context.BootRecords.Add(record);
            await context.SaveChangesAsync(stoppingToken);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Recorded start {Id}: version {Version} in {Environment} on {Provider}.",
                    record.Id.ToString(), record.Version, record.Environment, record.DatabaseProvider);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A boot record that cannot be written is a diagnostic that is lost,
            // not a request that fails. The service stays up; the log says why
            // the list will have a gap in it.
            logger.LogError(exception, "Could not record this start. The service is unaffected; the boot list will be missing a row.");
        }
    }
}
