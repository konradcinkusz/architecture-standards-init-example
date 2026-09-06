using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArchitectureStandardsInitExample.ServiceDefaults;

/// <summary>
/// P4 — the provider is a configuration switch, not a compile-time decision.
/// With no connection string the service falls back to the in-memory provider,
/// which is what makes <c>git clone &amp;&amp; dotnet run</c> work with zero cloud
/// credentials (P8) and what lets tests run without a container (P13).
/// </summary>
public static class DatabaseProviderExtensions
{
    public enum DatabaseProvider
    {
        InMemory,
        PostgreSql,
        SqlServer
    }

    /// <param name="connectionName">
    /// The connection-string name this service owns — and the only one it holds
    /// credentials for (P3). One database per service; physical co-location is a
    /// cost decision, the logical boundary is not.
    /// </param>
    /// <param name="inMemoryDatabaseName">
    /// The in-memory database used when no connection string is configured.
    /// </param>
    public static IServiceCollection AddDatabaseContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionName,
        string inMemoryDatabaseName)
        where TContext : DbContext
    {
        var provider = ResolveProvider(configuration, connectionName);
        var connectionString = NormalizeConnectionString(configuration.GetConnectionString(connectionName));

        services.AddDbContext<TContext>(options =>
        {
            switch (provider)
            {
                case DatabaseProvider.PostgreSql:
                    options.UseNpgsql(connectionString, npgsql =>
                    {
                        npgsql.EnableRetryOnFailure(10, TimeSpan.FromSeconds(30), null);
                        npgsql.CommandTimeout(60);
                        npgsql.MigrationsAssembly(typeof(TContext).Assembly.FullName);
                    });
                    break;

                case DatabaseProvider.SqlServer:
                    options.UseSqlServer(connectionString, sql =>
                    {
                        sql.EnableRetryOnFailure(10, TimeSpan.FromSeconds(30), null);
                        sql.CommandTimeout(60);
                        sql.MigrationsAssembly(typeof(TContext).Assembly.FullName);
                    });
                    break;

                default:
                    options.UseInMemoryDatabase(inMemoryDatabaseName);
                    break;
            }
        });

        return services;
    }

    /// <summary>
    /// DATABASE_PROVIDER selects the provider; the absence of a connection string
    /// overrides it. Asking for PostgreSQL without giving it an address is a
    /// misconfiguration, and falling back is more useful than refusing to start.
    /// </summary>
    public static DatabaseProvider ResolveProvider(IConfiguration configuration, string connectionName)
    {
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString(connectionName)))
        {
            return DatabaseProvider.InMemory;
        }

        return configuration["DATABASE_PROVIDER"]?.Trim().ToLowerInvariant() switch
        {
            "postgresql" or "postgres" or "npgsql" => DatabaseProvider.PostgreSql,
            "sqlserver" or "mssql" => DatabaseProvider.SqlServer,
            _ => DatabaseProvider.InMemory
        };
    }

    /// <summary>
    /// Fly's <c>.flycast</c> address routes through the proxy, which a long-lived
    /// database connection does not need and which adds a hop; the private 6PN
    /// name is the direct route (FLY-IO §6). The raised timeout covers a cold
    /// start on a machine the proxy has just woken.
    /// </summary>
    public static string? NormalizeConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        var normalized = connectionString.Replace(".flycast", ".internal", StringComparison.OrdinalIgnoreCase);

        if (!normalized.Contains("Timeout", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.TrimEnd(';') + ";Timeout=30;Command Timeout=60";
        }

        return normalized;
    }
}
