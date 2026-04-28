using Microsoft.AspNetCore.Mvc;
using webhook_gateway.Services;

namespace webhook_gateway.Controllers
{
    /// <summary>
    /// Receives 360dialog webhook calls for the PRAN-RFL Sales Support Chatbot
    /// and forwards them to http://localhost:8042 unchanged.
    ///
    /// Register this URL in 360dialog:
    ///   https://webhook.prangroup.com/webhook/sales-support-webhook
    /// </summary>
    [ApiController]
    [Route("webhook")]
    public class SalesSupportWebhookController : ControllerBase
    {
        private readonly ForwardingService _forwarding;
        private readonly ILogger<SalesSupportWebhookController> _logger;

        public SalesSupportWebhookController(
            ForwardingService forwarding,
            ILogger<SalesSupportWebhookController> logger)
        {
            _forwarding = forwarding;
            _logger     = logger;
        }

        /// <summary>
        /// GET /webhook/sales-support-webhook
        /// 360dialog calls this to verify the webhook URL.
        /// The verification challenge is forwarded to the Sales Support Chatbot (port 8042).
        /// </summary>
        [HttpGet("sales-support-webhook")]
        public async Task<IActionResult> Verify(CancellationToken ct)
        {
            _logger.LogInformation("[SalesSupport] Webhook verification request received");
            var result = await _forwarding.ForwardToSalesSupportAsync(Request, ct);
            return Content(result.Body, result.ContentType);
        }

        /// <summary>
        /// POST /webhook/sales-support-webhook
        /// Receives every incoming WhatsApp message / status update from 360dialog
        /// and forwards the raw payload to the Sales Support Chatbot (port 8042).
        /// </summary>
        [HttpPost("sales-support-webhook")]
        public async Task<IActionResult> Receive(CancellationToken ct)
        {
            _logger.LogInformation("[SalesSupport] Incoming WhatsApp event");
            var result = await _forwarding.ForwardToSalesSupportAsync(Request, ct);
            return StatusCode(result.StatusCode, result.Body);
        }
    }
}
