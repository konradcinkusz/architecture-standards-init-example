namespace ArchitectureStandardsInitExample.Contracts;

/// <summary>
/// One error shape for every failure this API returns, so a client writes one
/// handler rather than one per endpoint (SERVICE-API-PATTERNS §2).
/// </summary>
/// <param name="Error">A stable, machine-readable code. Clients branch on this.</param>
/// <param name="Message">A human-readable sentence. Clients display this; they do not parse it.</param>
/// <param name="Details">Field-level detail, when the failure was a validation failure.</param>
public sealed record ApiError(
    string Error,
    string Message,
    IReadOnlyDictionary<string, string[]>? Details = null)
{
    public static ApiError NotFound(string what, object id) =>
        new("not_found", $"No {what} with id '{id}'.");

    public static ApiError Validation(IReadOnlyDictionary<string, string[]> details) =>
        new("validation_failed", "One or more values are not acceptable.", details);
}
