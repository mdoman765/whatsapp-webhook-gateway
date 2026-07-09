using webhook_gateway.Models;

namespace webhook_gateway.Services
{
    public interface ICrmRoutingService
    {
        Task<CrmRoutingResult> RouteShopAssignmentAsync(
            CrmShopAssignmentRequest request, CancellationToken ct = default);
    }

    /// <summary>Carries the downstream chatbot's response back to the controller.</summary>
    public record CrmRoutingResult(int StatusCode, string ContentType, string Body);
}