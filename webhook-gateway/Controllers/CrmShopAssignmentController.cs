using Microsoft.AspNetCore.Mvc;
using webhook_gateway.Models;
using webhook_gateway.Services;

namespace webhook_gateway.Controllers
{
    /// <summary>
    /// Receives shop-assignment requests from the CRM and routes them
    /// to the correct chatbot backend based on ChatbotName.
    ///
    /// Public endpoint:
    ///   POST https://webhook.prangroup.com/api/crm/shop-assignment
    /// </summary>
    [ApiController]
    [Route("api/crm")]
    public class CrmShopAssignmentController : ControllerBase
    {
        private readonly ICrmRoutingService _routingService;
        private readonly ILogger<CrmShopAssignmentController> _logger;

        public CrmShopAssignmentController(
            ICrmRoutingService routingService,
            ILogger<CrmShopAssignmentController> logger)
        {
            _routingService = routingService;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/crm/shop-assignment
        /// Body: { "chatbotName": "UAE_Chatbot", "shopCode": "123456678" }
        /// </summary>
        [HttpPost("shop-assignment")]
        public async Task<IActionResult> AssignShop(
            [FromBody] CrmShopAssignmentRequest request, CancellationToken ct)
        {
            _logger.LogInformation(
                "[CRM] Shop assignment received. Chatbot={ChatbotName} ShopCode={ShopCode}",
                request.ChatbotName, request.ShopCode);

            var result = await _routingService.RouteShopAssignmentAsync(request, ct);

            // ContentResult (not StatusCode+string) so the downstream JSON
            // passes through raw, instead of being re-serialized as a JSON string.
            return new ContentResult
            {
                StatusCode = result.StatusCode,
                Content = result.Body,
                ContentType = result.ContentType
            };
        }
    }
}