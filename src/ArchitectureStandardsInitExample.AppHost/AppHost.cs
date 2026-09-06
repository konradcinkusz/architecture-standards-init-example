// P1 — every resource the system needs, plus the edges between them. A developer
// clones the repository and runs:
//
//     dotnet run --project src/ArchitectureStandardsInitExample.AppHost
//
// and gets Postgres, the API and the frontend, wired together, with a dashboard.

var builder = DistributedApplication.CreateBuilder(args);

// P5 — no secret is ever a literal here. The value comes from
// `dotnet user-secrets`, where scripts/setup.sh generated it; the AppHost only
// knows its name. `secret: true` keeps it out of the dashboard and the logs.
var postgresPassword = builder.AddParameter("postgres-password", secret: true);

// P3 — one instance, one database per service. Physical co-location is a cost
// decision; the logical boundary is what must not be crossed. `apidb` has
// exactly one owner, and no other service is given a connection string to it.
var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithDataVolume()
    .WithPgAdmin();

var apiDatabase = postgres.AddDatabase("apidb");

var api = builder.AddProject<Projects.ArchitectureStandardsInitExample_Api>("api")
    .WithReference(apiDatabase)
    .WaitFor(apiDatabase)
    .WithEnvironment("DATABASE_PROVIDER", "PostgreSQL")
    .WithHttpHealthCheck("/health");

// The frontend's server side is the only thing that calls the API; the browser
// talks only to the frontend's own origin (FRONTEND-BFF §1). WithReference gives
// the Next.js process the service-discovery variables the BFF proxy's candidate
// ladder reads.
builder.AddNextJsApp("web", "../../web/app")
    // WithPnpm is not optional here. Without it the JavaScript hosting
    // integration picks its own package manager and runs `npm install` in
    // web/app — which creates a second node_modules and a package-lock.json
    // beside a pnpm workspace, i.e. exactly the mixed npm/pnpm tree
    // FRONTEND-BFF §7 calls an anti-pattern with no upside. Caught by running
    // this AppHost, not by reading it.
    .WithPnpm()
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();
