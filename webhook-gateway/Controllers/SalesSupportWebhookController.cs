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

        public SalesSupportWebhookController(
            ForwardingService forwarding,
            ILogger<SalesSupportWebhookController> logger)
        {
            _forwarding = forwarding;
            _logger     = logger;
        }

        
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
