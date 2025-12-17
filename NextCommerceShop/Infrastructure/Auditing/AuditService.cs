using NextCommerceShop.Application.Abstractions;
using NextCommerceShop.Data;
using NextCommerceShop.Models;

namespace NextCommerceShop.Infrastructure.Auditing;

public sealed class AuditService : IAuditService
{
    private readonly AppDbContext _db;
    private readonly IActorContext _actor;

    public AuditService(AppDbContext db, IActorContext actor)
    {
        _db = db;
        _actor = actor;
    }

    public async Task WriteAsync(
        string action,
        string? targetUserId = null,
        string? targetEmail = null,
        object? data = null,
        CancellationToken ct = default)
    {
        try
        {
            var log = new AuditLog
            {
                ActorUserId = _actor.UserId ?? "",
                ActorEmail = _actor.Email ?? "",
                Action = action,
                TargetUserId = targetUserId ?? "",
                TargetEmail = targetEmail ?? "",
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            // NEVER throw from auditing
            // (optional: log internally later)
        }
    }
}
