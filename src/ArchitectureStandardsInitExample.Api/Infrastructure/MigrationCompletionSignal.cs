namespace ArchitectureStandardsInitExample.Api.Infrastructure;

/// <summary>
/// Migrations run after Kestrel starts (P4), so every other hosted service races
/// the schema unless it waits (SERVICE-API-PATTERNS §7). This is what it waits
/// on. Without it the first reaper or seeder dies on a missing table once, at
/// 3 a.m., unreproducibly.
/// </summary>
public sealed class MigrationCompletionSignal
{
    private readonly TaskCompletionSource _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task WaitAsync(CancellationToken cancellationToken) =>
        _completion.Task.WaitAsync(cancellationToken);

    public void MarkComplete() => _completion.TrySetResult();

    /// <summary>
    /// A failed migration must release the waiters rather than leaving them
    /// blocked forever — they then fail with the real reason instead of hanging.
    /// </summary>
    public void MarkFailed(Exception exception) => _completion.TrySetException(exception);
}
