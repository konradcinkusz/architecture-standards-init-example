namespace ArchitectureStandardsInitExample.Contracts;

/// <summary>
/// One recorded start of the API. This is the published shape another service —
/// or the frontend's server side — compiles against; changing a member here is a
/// contract change, which is exactly why it lives in this project rather than in
/// the service that produces it.
/// </summary>
/// <param name="Id">Stable identity of this start.</param>
/// <param name="RecordedAt">When the row was written, in UTC.</param>
/// <param name="Version">The informational version of the assembly that started.</param>
/// <param name="Environment">The ASP.NET Core hosting environment it started in.</param>
/// <param name="DatabaseProvider">Which persistence provider was resolved (P4).</param>
/// <param name="DegradedIntegrations">
/// The optional integrations that were absent at start (P8). Empty means every
/// optional integration this build knows about was configured.
/// </param>
public sealed record BootRecordResponse(
    Guid Id,
    DateTimeOffset RecordedAt,
    string Version,
    string Environment,
    string DatabaseProvider,
    IReadOnlyList<string> DegradedIntegrations);
