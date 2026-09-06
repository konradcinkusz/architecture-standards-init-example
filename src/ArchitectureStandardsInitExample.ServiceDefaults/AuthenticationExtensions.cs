using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ArchitectureStandardsInitExample.ServiceDefaults;

/// <summary>
/// P5 — validation only. Tokens are verified against the identity service's
/// published JWKS; this assembly holds no key material and mints nothing.
/// Exactly one service in the estate holds a signing key, and it is never this
/// one: a symmetric secret shared between services means verify = mint, and any
/// holder can forge a token for any user.
/// <para>
/// This system currently has no user accounts (ADR-0004), so no service in this
/// repository calls this method. It stays in the kernel because P2's table
/// defines the kernel's shape, not today's consumer list — a service opts in
/// line by line, and the line to opt in with has to exist.
/// </para>
/// </summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authority = configuration["Jwt:Authority"];
        var audience = configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(authority))
        {
            throw new InvalidOperationException(
                "Jwt:Authority is not configured. A service that calls AddJwtAuthentication is " +
                "asserting that its endpoints are protected; starting without an authority would " +
                "leave them open. This is not an optional integration (P8) — it is the boundary.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // The authority's JWKS is fetched here, and re-fetched when the
                // cache expires — which is why an issuer is on the synchronous
                // request path of every validator and pins a machine (FLY-IO §7).
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = !authority.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase);

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authority,
                    ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization();
        return services;
    }
}
