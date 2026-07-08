using Microsoft.AspNetCore.Mvc;
using webhook_gateway.Services;

namespace webhook_gateway.Controllers
{
    [ApiController]
    [Route("webhook")]
    public class SalesSupportWebhookController : ControllerBase
    {
        private readonly ForwardingService _forwarding;
        private readonly ILogger<SalesSupportWebhookController> _logger;
        private readonly IConfiguration _configuration;

        public SalesSupportWebhookController(
            ForwardingService forwarding,
            ILogger<SalesSupportWebhookController> logger,
            IConfiguration configuration)
        {
            _forwarding = forwarding;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// GET /webhook/sales-support-webhook
        /// Meta sends this request to verify the webhook endpoint.
        /// Must respond with hub.challenge if the verify token matches.
        /// </summary>
        [HttpGet("sales-support-webhook")]
        public IActionResult Verify(
            [FromQuery(Name = "hub.mode")] string? mode,
            [FromQuery(Name = "hub.verify_token")] string? verifyToken,
            [FromQuery(Name = "hub.challenge")] string? challenge)
        {
            _logger.LogInformation("[SalesSupport] Webhook verification request received. Mode={Mode}", mode);

            var expectedToken = _configuration["WhatsApp:VerifyToken"];

            if (mode == "subscribe" && verifyToken == expectedToken)
            {
                _logger.LogInformation("[SalesSupport] Webhook verified successfully.");
                return Content(challenge ?? string.Empty, "text/plain");
            }

            _logger.LogWarning("[SalesSupport] Webhook verification failed. Token mismatch or wrong mode.");
            return Forbid();
        }

        /// <summary>
        /// POST /webhook/sales-support-webhook
        /// Receives every incoming WhatsApp message / status update from Meta
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