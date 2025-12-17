using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NextCommerceShop.Application.Abstractions;

namespace NextCommerceShop.Infrastructure.Identity;

public sealed class HttpActorContext : IActorContext
{
    private readonly IHttpContextAccessor _http;

    public HttpActorContext(IHttpContextAccessor http)
    {
        _http = http;
    }

    public string? UserId =>
        _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Email =>
        _http.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
        ?? _http.HttpContext?.User?.Identity?.Name;

    public string CorrelationId =>
        _http.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");
}
