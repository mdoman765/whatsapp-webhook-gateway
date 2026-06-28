using Microsoft.AspNetCore.Mvc;
using webhook_gateway.Services;

namespace webhook_gateway.Controllers
{
    /// <summary>
    /// Receives 360dialog webhook calls for the KSA Chatbot and forwards them
    /// to http://localhost:8044 unchanged.
    ///
    /// Register this URL in 360dialog:
    ///   https://webhook.prangroup.com/webhook/ksa-webhook
    /// </summary>
    [ApiController]
    [Route("webhook")]
    public class KsaWebhookController : ControllerBase
    {
        private readonly ForwardingService _forwarding;
        private readonly ILogger<KsaWebhookController> _logger;

        public KsaWebhookController(
            ForwardingService forwarding,
            ILogger<KsaWebhookController> logger)
        {
            _forwarding = forwarding;
            _logger = logger;
        }

        /// <summary>
        /// GET /webhook/ksa-webhook
        /// 360dialog calls this to verify the webhook URL.
        /// The verification challenge is forwarded to the KSA Chatbot (port 8044).
        /// </summary>
        [HttpGet("ksa-webhook")]
        public async Task<IActionResult> Verify(CancellationToken ct)
        {
            _logger.LogInformation("[KSA] Webhook verification request received");
            var result = await _forwarding.ForwardToKsaAsync(Request, ct);
            return Content(result.Body, result.ContentType);
        }

        /// <summary>
        /// POST /webhook/ksa-webhook
        /// Receives every incoming WhatsApp message / status update from 360dialog
        /// and forwards the raw payload to the KSA Chatbot (port 8044).
        /// </summary>
        [HttpPost("ksa-webhook")]
        public async Task<IActionResult> Receive(CancellationToken ct)
        {
            _logger.LogInformation("[KSA] Incoming WhatsApp event");
            var result = await _forwarding.ForwardToKsaAsync(Request, ct);
            return StatusCode(result.StatusCode, result.Body);
        }
    }
}
