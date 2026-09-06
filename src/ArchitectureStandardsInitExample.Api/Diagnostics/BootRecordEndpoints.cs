using ArchitectureStandardsInitExample.Api.Infrastructure;
using ArchitectureStandardsInitExample.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace ArchitectureStandardsInitExample.Api.Diagnostics;

/// <summary>
/// Transport only: bind, authorize, delegate (P9). Every rule here is from
/// SERVICE-API-PATTERNS — a group at the right trust level, clamped list inputs,
/// aggregates in one round trip, and one uniform error shape.
/// </summary>
public static class BootRecordEndpoints
{
    /// <summary>Stable operation ids, so a generated client does not rename itself on a refactor.</summary>
    public static class OperationNames
    {
        public const string ListBootRecords = "ListBootRecords";
        public const string GetBootRecord = "GetBootRecord";
    }

    public const int MaxPageSize = 100;

    public static IEndpointRouteBuilder MapBootRecordEndpoints(this IEndpointRouteBuilder app)
    {
        // The authorization triad (SERVICE-API-PATTERNS §2) collapses to one
        // group here because this system has no accounts (ADR-0004). When it
        // gains them, the authenticated and admin groups are declared alongside
        // this one, in this file, where a missing RequireAuthorization is
        // greppable rather than invisible in an attribute somewhere.
        var publicApi = app.MapGroup("/api/boots").WithTags("Diagnostics");

        publicApi.MapGet("/", ListAsync)
            .WithName(OperationNames.ListBootRecords)
            .WithSummary("Every recorded start of this service, newest first.")
            .Produces<PagedResponse<BootRecordResponse>>();

        publicApi.MapGet("/{id:guid}", GetAsync)
            .WithName(OperationNames.GetBootRecord)
            .WithSummary("One recorded start.")
            .Produces<BootRecordResponse>()
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(
        ApiDbContext database,
        CancellationToken cancellationToken,
        int page = 1,
        int limit = 20)
    {
        // Clamping is a DoS control, not a nicety: an unclamped `limit=2000000`
        // is a one-line outage (SERVICE-API-PATTERNS §4).
        page = Math.Max(1, page);
        limit = Math.Clamp(limit, 1, MaxPageSize);

        var query = database.BootRecords.AsNoTracking();

        // One round trip for the count, one for the page — not one Count() per
        // statistic, and never a materialized list filtered in memory.
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(record => record.RecordedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(record => ToResponse(record))
            .ToListAsync(cancellationToken);

        return Results.Ok(new PagedResponse<BootRecordResponse>(items, page, limit, total));
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ApiDbContext database,
        CancellationToken cancellationToken)
    {
        var record = await database.BootRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        return record is null
            ? Results.NotFound(ApiError.NotFound("boot record", id))
            : Results.Ok(ToResponse(record));
    }

    private static BootRecordResponse ToResponse(BootRecord record) => new(
        record.Id,
        record.RecordedAt,
        record.Version,
        record.Environment,
        record.DatabaseProvider,
        string.IsNullOrEmpty(record.DegradedIntegrations)
            ? []
            : record.DegradedIntegrations.Split(','));
}
