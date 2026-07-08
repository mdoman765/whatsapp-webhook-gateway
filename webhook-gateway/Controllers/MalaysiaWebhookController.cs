using Microsoft.AspNetCore.Mvc;
using webhook_gateway.Services;

namespace webhook_gateway.Controllers
{
    /// <summary>
    /// Receives 360dialog webhook calls for the Malaysia Chatbot and forwards them
    /// to http://localhost:8041 unchanged.
    ///
    /// Register this URL in 360dialog:
    ///   https://webhook.prangroup.com/webhook/malaysia-webhook
    /// </summary>
    [ApiController]
    [Route("webhook")]
    public class MalaysiaWebhookController : ControllerBase
    {
        private readonly ForwardingService _forwarding;
        private readonly ILogger<MalaysiaWebhookController> _logger;
        private readonly IConfiguration _configuration;

        public MalaysiaWebhookController(
            ForwardingService forwarding,
            ILogger<MalaysiaWebhookController> logger,
            IConfiguration configuration)
        {
            _forwarding = forwarding;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// GET /webhook/malaysia-webhook
        /// 360dialog/Meta sends this request to verify the webhook endpoint.
        /// Must respond with hub.challenge if the verify token matches.
        /// </summary>
        [HttpGet("malaysia-webhook")]
        public IActionResult Verify(
            [FromQuery(Name = "hub.mode")] string? mode,
            [FromQuery(Name = "hub.verify_token")] string? verifyToken,
            [FromQuery(Name = "hub.challenge")] string? challenge)
        {
            _logger.LogInformation("[Malaysia] Webhook verification request received. Mode={Mode}", mode);

            var expectedToken = _configuration["WhatsApp:VerifyToken"];

            if (mode == "subscribe" && verifyToken == expectedToken)
            {
                _logger.LogInformation("[Malaysia] Webhook verified successfully.");
                return Content(challenge ?? string.Empty, "text/plain");
            }

            _logger.LogWarning("[Malaysia] Webhook verification failed. Token mismatch or wrong mode.");
            return Forbid();
        }

        /// <summary>
        /// POST /webhook/malaysia-webhook
        /// Receives every incoming WhatsApp message / status update from 360dialog
        /// and forwards the raw payload to the Malaysia Chatbot (port 8041).
        /// </summary>
        [HttpPost("malaysia-webhook")]
        public async Task<IActionResult> Receive(CancellationToken ct)
        {
            _logger.LogInformation("[Malaysia] Incoming WhatsApp event");
            var result = await _forwarding.ForwardToMalaysiaAsync(Request, ct);
            return StatusCode(result.StatusCode, result.Body);
        }
    }
}