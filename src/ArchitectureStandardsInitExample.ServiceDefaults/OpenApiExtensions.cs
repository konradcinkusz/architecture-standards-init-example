using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;

namespace ArchitectureStandardsInitExample.ServiceDefaults;

/// <summary>
/// One OpenAPI document shape for the estate, with the bearer scheme declared so
/// a generated client knows how to authenticate against services that require it.
/// </summary>
public static class OpenApiExtensions
{
    public static IServiceCollection AddSwaggerWithJwt(
        this IServiceCollection services,
        string title,
        string version,
        string description)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(version, new OpenApiInfo
            {
                Title = title,
                Version = version,
                Description = description
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "A token issued by the estate's identity service. Validated against its JWKS; never minted here (P5)."
            });
        });

        return services;
    }

    /// <summary>
    /// The document is served outside Production by default: an unauthenticated
    /// description of every endpoint is a reconnaissance aid, and publishing it
    /// should be a decision rather than a default.
    /// </summary>
    public static WebApplication MapOpenApi(this WebApplication app, string version, string title)
    {
        if (!app.Environment.IsProduction())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options => options.SwaggerEndpoint($"/swagger/{version}/swagger.json", title));
        }

        return app;
    }
}
