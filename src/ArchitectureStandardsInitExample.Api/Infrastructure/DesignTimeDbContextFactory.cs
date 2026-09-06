using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ArchitectureStandardsInitExample.Api.Infrastructure;

/// <summary>
/// Used by <c>dotnet ef</c> only, never at runtime. It exists so generating a
/// migration does not start the application, and so the provider a migration is
/// scaffolded against is explicit in the repository rather than dependent on
/// whatever environment variables the author happened to have set.
/// <para>
/// The connection string below is a design-time placeholder: EF needs a provider,
/// not a reachable server, to scaffold a migration. Nothing connects to it.
/// </para>
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApiDbContext>
{
    public ApiDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseNpgsql("Host=design-time-only;Database=apidb", npgsql =>
                npgsql.MigrationsAssembly(typeof(ApiDbContext).Assembly.FullName))
            .Options;

        return new ApiDbContext(options);
    }
}
