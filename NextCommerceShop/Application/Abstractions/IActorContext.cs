namespace NextCommerceShop.Application.Abstractions;

public interface IActorContext
{
    string? UserId { get; }
    string? Email { get; }
    string CorrelationId { get; }
}
