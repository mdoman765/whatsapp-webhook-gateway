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
        private readonly IConfiguration _configuration;

        public UaeWebhookController(
            ForwardingService forwarding,
            ILogger<UaeWebhookController> logger,
            IConfiguration configuration)
        {
            _forwarding = forwarding;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// GET /webhook/whatsapp-webhook
        /// Meta/360dialog sends this request to verify the webhook endpoint.
        /// Must respond with hub.challenge if the verify token matches.
        /// </summary>
        [HttpGet("whatsapp-webhook")]
        public IActionResult Verify(
            [FromQuery(Name = "hub.mode")] string? mode,
            [FromQuery(Name = "hub.verify_token")] string? verifyToken,
            [FromQuery(Name = "hub.challenge")] string? challenge)
        {
            _logger.LogInformation("[UAE] Webhook verification request received. Mode={Mode}", mode);

            var expectedToken = _configuration["WhatsApp:VerifyToken"];

            if (mode == "subscribe" && verifyToken == expectedToken)
            {
                _logger.LogInformation("[UAE] Webhook verified successfully.");
                return Content(challenge ?? string.Empty, "text/plain");
            }

            _logger.LogWarning("[UAE] Webhook verification failed. Token mismatch or wrong mode.");
            return Forbid();
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