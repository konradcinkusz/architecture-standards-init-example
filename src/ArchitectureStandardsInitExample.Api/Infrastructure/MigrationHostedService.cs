using ArchitectureStandardsInitExample.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ArchitectureStandardsInitExample.Api.Infrastructure;

/// <summary>
/// P4 — schema moves by <c>MigrateAsync</c> from provider-specific migrations,
/// applied by a hosted service <em>after</em> Kestrel starts, so health probes
/// answer while schema work is in flight and a slow migration is not read as a
/// failed deploy.
/// <para>
/// <c>EnsureCreated</c> appears exactly once below, on the InMemory path, where
/// there are no migrations to apply. On a real provider it records no migration
/// at all, which freezes the schema at first-boot state while migrations
/// accumulate in code — the estate has a live system in exactly that condition.
/// </para>
/// </summary>
public sealed class MigrationHostedService(
    IServiceScopeFactory scopeFactory,
    MigrationCompletionSignal signal,
    IConfiguration configuration,
    ILogger<MigrationHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApiDbContext>();

            var provider = DatabaseProviderExtensions.ResolveProvider(configuration, "apidb");
            if (provider == DatabaseProviderExtensions.DatabaseProvider.InMemory)
            {
                await context.Database.EnsureCreatedAsync(stoppingToken);
                logger.LogInformation("In-memory database created. No migrations to apply.");
            }
            else if (provider == DatabaseProviderExtensions.DatabaseProvider.SqlServer)
            {
                // The kernel offers both providers because P2's table defines the
                // kernel's shape. This service ships PostgreSQL migrations only,
                // because PostgreSQL is what it deploys on (ADR-0005). Running
                // Npgsql migrations against SQL Server would fail deep inside EF
                // with a syntax error; failing here says what is actually wrong.
                throw new NotSupportedException(
                    "DATABASE_PROVIDER=SqlServer was selected, but this service ships PostgreSQL " +
                    "migrations only. Targeting SQL Server needs its own migrations assembly — see " +
                    "docs/adr/0005-postgresql-migrations-only.md. Unset DATABASE_PROVIDER to run " +
                    "against the in-memory provider instead.");
            }
            else
            {
                var pending = (await context.Database.GetPendingMigrationsAsync(stoppingToken)).ToList();
                if (pending.Count > 0 && logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Applying {Count} pending migration(s): {Migrations}",
                        pending.Count, string.Join(", ", pending));
                }

                await context.Database.MigrateAsync(stoppingToken);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Schema is up to date on {Provider}.", provider.ToString());
                }
            }

            signal.MarkComplete();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Release the waiters with the real reason. Swallowing this leaves
            // every other hosted service blocked on a task that never completes.
            logger.LogError(exception, "Schema initialization failed. Dependent background work will fail with this reason.");
            signal.MarkFailed(exception);
        }
    }
}
