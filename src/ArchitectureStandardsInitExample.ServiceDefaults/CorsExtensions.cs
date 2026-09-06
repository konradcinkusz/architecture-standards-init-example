using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArchitectureStandardsInitExample.ServiceDefaults;

/// <summary>
/// One named CORS policy for the estate, read from configuration so the allowed
/// origins differ per environment without a rebuild (P5, P12).
/// <para>
/// No service in this repository calls this today: the browser talks only to the
/// frontend's own origin and the frontend's server side calls the API
/// (FRONTEND-BFF §1), so there is no cross-origin request to permit. A value in
/// <c>Cors:AllowedOrigins</c> is a deliberate decision to widen that.
/// </para>
/// </summary>
public static class CorsExtensions
{
    public static class CorsPolicies
    {
        public const string Frontend = "frontend";
    }

    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration,
        string policyName)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(policyName, policy =>
        {
            if (origins.Length == 0)
            {
                // No origins configured means no cross-origin access, not "any".
                // AllowAnyOrigin as a default is how a development convenience
                // reaches production.
                policy.WithOrigins().DisallowCredentials();
                return;
            }

            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }));

        return services;
    }
}
