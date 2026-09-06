namespace ArchitectureStandardsInitExample.Api.Diagnostics;

/// <summary>
/// One row per start of this service: which build came up, in which environment,
/// against which provider, and which optional integrations were missing.
/// <para>
/// This is the template's one thin vertical slice, and it is deliberately
/// operational rather than a product domain — inventing entities for a product
/// nobody has described yet produces code the first ticket deletes. It earns its
/// place by being the cheapest end-to-end proof that the schema really migrated
/// and that the running image is the one you think it is.
/// </para>
/// </summary>
public sealed class BootRecord
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public DateTimeOffset RecordedAt { get; init; } = DateTimeOffset.UtcNow;
    public required string Version { get; init; }
    public required string Environment { get; init; }
    public required string DatabaseProvider { get; init; }

    /// <summary>
    /// Comma-separated integration names. A join table for a list that is read
    /// whole and never queried by member would be structure for its own sake.
    /// </summary>
    public string DegradedIntegrations { get; init; } = string.Empty;
}
