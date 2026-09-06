namespace ArchitectureStandardsInitExample.Contracts;

/// <summary>
/// The envelope every list endpoint returns. <paramref name="Limit"/> is the
/// limit that was actually applied, not the one that was asked for — every list
/// endpoint clamps, and a client that asked for 2,000,000 rows deserves to be
/// told it is getting 100 (SERVICE-API-PATTERNS §4).
/// </summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int Limit,
    int Total)
{
    public int TotalPages => Limit <= 0 ? 0 : (int)Math.Ceiling(Total / (double)Limit);
    public bool HasMore => Page * Limit < Total;
}
