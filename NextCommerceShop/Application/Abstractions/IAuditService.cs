namespace NextCommerceShop.Application.Abstractions;

public interface IAuditService
{
    Task WriteAsync(
        string action,
        string? targetUserId = null,
        string? targetEmail = null,
        object? data = null,
        CancellationToken ct = default);
}
