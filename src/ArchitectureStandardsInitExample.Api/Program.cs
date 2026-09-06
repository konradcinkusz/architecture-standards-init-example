using ArchitectureStandardsInitExample.Api.Diagnostics;
using ArchitectureStandardsInitExample.Api.Infrastructure;
using ArchitectureStandardsInitExample.ServiceDefaults;

// P9 — Program.cs is a manifest. Each block is one call into the service's own
// ServiceCollectionExtensions; nothing here is configuration code.
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddApiPersistence(builder.Configuration);
builder.Services.AddIntegrationReporting();
builder.Services.AddDiagnosticsSlice();
builder.Services.AddSwaggerWithJwt(
    title: "architecture-standards-init-example API",
    version: "v1",
    description: "The one service in this system. Owns apidb; exposes /health, /alive and the boot log.");

// Deliberately NOT called here, and both absences are decisions rather than
// omissions (ADR-0004):
//   builder.Services.AddJwtAuthentication(builder.Configuration);
//     — this system has no user accounts, so there is nothing to authenticate.
//   builder.Services.AddCorsPolicy(builder.Configuration, CorsPolicies.Frontend);
//     — the browser talks only to the frontend's own origin, and the frontend's
//       server side calls this API, so there is no cross-origin request to allow.
// Both extension methods exist in the kernel because P2's table defines the
// kernel's shape; a service opts in line by line, and this one opts into neither.

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapOpenApi("v1", "architecture-standards-init-example API");
app.MapBootRecordEndpoints();

app.Run();

/// <summary>Exposed so the test project can host this service in-process.</summary>
public partial class Program;
