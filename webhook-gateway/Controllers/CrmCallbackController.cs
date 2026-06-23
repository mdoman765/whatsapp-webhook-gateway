using Microsoft.AspNetCore.Mvc;
using webhook_gateway.Services;

namespace webhook_gateway.Controllers
{
    /// <summary>
    /// Receives CRM ticket-status callbacks at the public gateway URL and
    /// forwards them — body unchanged — to the UAE Chatbot backend.
    ///
    /// Public endpoint (what you register in the CRM):
    ///   POST https://webhook.prangroup.com/webhook/crm-status
    ///
    /// Forwarded to:
    ///   POST http://localhost:8041/api/crm/ticket-status
    /// </summary>
    [ApiController]
    [Route("webhook")]
    public class CrmCallbackController : ControllerBase
    {
        private readonly ForwardingService _forwarding;
        private readonly ILogger<CrmCallbackController> _logger;

        public CrmCallbackController(
            ForwardingService forwarding,
            ILogger<CrmCallbackController> logger)
        {
            _forwarding = forwarding;
            _logger = logger;
        }

        /// <summary>
        /// POST /webhook/crm-status
        /// CRM pushes { "ExternalTicketId": "...", "Status": "..." } here.
        /// The raw body is forwarded to http://localhost:8041/api/crm/ticket-status.
        /// </summary>
        [HttpPost("crm-status")]
        public async Task<IActionResult> Receive(CancellationToken ct)
        {
            _logger.LogInformation("[CrmCallback] Incoming CRM status callback");
            var result = await _forwarding.ForwardToCrmCallbackAsync(Request, ct);
            return StatusCode(result.StatusCode, result.Body);
        }
    }
}
