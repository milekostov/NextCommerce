using System;

namespace NextCommerceShop.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        // Who performed the action
        public string ActorUserId { get; set; } = string.Empty;
        public string ActorEmail { get; set; } = string.Empty;

        // What was done
        public string Action { get; set; } = string.Empty;

        // On whom / what
        public string TargetUserId { get; set; } = string.Empty;
        public string TargetEmail { get; set; } = string.Empty;

        // When
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
