using Microsoft.AspNetCore.Mvc;
using webhook_gateway.Services;

namespace webhook_gateway.Controllers
{
    /// <summary>
    /// Receives 360dialog webhook calls for the UAE Chatbot and forwards them
    /// to http://localhost:8041 unchanged.
    ///
    /// Register this URL in 360dialog:
    ///   https://webhook.prangroup.com/webhook/whatsapp-webhook
    /// </summary>
    [ApiController]
    [Route("webhook")]
    public class UaeWebhookController : ControllerBase
    {
        private readonly ForwardingService _forwarding;
        private readonly ILogger<UaeWebhookController> _logger;

        public UaeWebhookController(
            ForwardingService forwarding,
            ILogger<UaeWebhookController> logger)
        {
            _forwarding = forwarding;
            _logger     = logger;
        }

        /// <summary>
        /// GET /webhook/whatsapp-webhook
        /// 360dialog calls this to verify the webhook URL.
        /// The verification challenge is forwarded to the UAE Chatbot (port 8041).
        /// </summary>
        [HttpGet("whatsapp-webhook")]
        public async Task<IActionResult> Verify(CancellationToken ct)
        {
            _logger.LogInformation("[UAE] Webhook verification request received");
            var result = await _forwarding.ForwardToUaeAsync(Request, ct);
            return Content(result.Body, result.ContentType);
        }

        /// <summary>
        /// POST /webhook/whatsapp-webhook
        /// Receives every incoming WhatsApp message / status update from 360dialog
        /// and forwards the raw payload to the UAE Chatbot (port 8041).
        /// </summary>
        [HttpPost("whatsapp-webhook")]
        public async Task<IActionResult> Receive(CancellationToken ct)
        {
            _logger.LogInformation("[UAE] Incoming WhatsApp event");
            var result = await _forwarding.ForwardToUaeAsync(Request, ct);
            return StatusCode(result.StatusCode, result.Body);
        }
    }
}
