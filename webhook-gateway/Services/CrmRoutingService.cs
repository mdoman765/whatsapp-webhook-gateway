using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using webhook_gateway.Models;
using webhook_gateway.Options;

namespace webhook_gateway.Services
{
    /// <summary>
    /// Routes CRM shop-assignment requests to the correct chatbot backend
    /// based on ChatbotName. Adding a new chatbot only requires a new entry
    /// under "ChatbotRouting" in appsettings.json — no changes here.
    /// </summary>
    public class CrmRoutingService : ICrmRoutingService
    {
        public const string HttpClientName = "CrmRoutingClient";

        private static readonly JsonSerializerOptions OutgoingJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IReadOnlyDictionary<string, ChatbotRoute> _routes;
        private readonly ILogger<CrmRoutingService> _logger;

        public CrmRoutingService(
            IHttpClientFactory httpClientFactory,
            IOptions<Dictionary<string, ChatbotRoute>> routeOptions,
            ILogger<CrmRoutingService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            // Case-insensitive lookup so "uae_chatbot" and "UAE_Chatbot" both work.
            _routes = new Dictionary<string, ChatbotRoute>(
                routeOptions.Value, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<CrmRoutingResult> RouteShopAssignmentAsync(
            CrmShopAssignmentRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.ShopCode))
                return BadRequest("ShopCode is required.");

            if (string.IsNullOrWhiteSpace(request.Phone))
                return BadRequest("Phone is required.");

            if (!_routes.TryGetValue(request.ChatbotName, out var route))
            {
                _logger.LogWarning(
                    "[CRM] Unknown ChatbotName '{ChatbotName}'. Known: {Known}",
                    request.ChatbotName, string.Join(", ", _routes.Keys));

                return BadRequest(
                    $"Unknown ChatbotName '{request.ChatbotName}'. " +
                    $"Valid values: {string.Join(", ", _routes.Keys)}");
            }

            // ── Forward the FULL request as-received ─────────────────────────
            // Includes chatbotName, shopCode, phone, and any additionalParameters.
            // e.g. { "chatbotName": "UAE_Chatbot", "shopCode": "154857845", "phone": "971581260024" }
            var json = JsonSerializer.Serialize(request, OutgoingJsonOptions);
            var targetUrl = $"{route.BaseUrl.TrimEnd('/')}{route.ShopAssignmentPath}";

            _logger.LogInformation(
                "[CRM] Routing shop assignment → {ChatbotName} at {Url} (Phone={Phone})",
                request.ChatbotName, targetUrl, request.Phone);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var httpClient = _httpClientFactory.CreateClient(HttpClientName);

            try
            {
                var response = await httpClient.PostAsync(targetUrl, content, ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                var contentType = response.Content.Headers.ContentType?.ToString()
                                   ?? "application/json";

                _logger.LogInformation(
                    "[CRM] {ChatbotName} responded {StatusCode}",
                    request.ChatbotName, (int)response.StatusCode);

                return new CrmRoutingResult((int)response.StatusCode, contentType, body);
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("[CRM] {ChatbotName} timed out", request.ChatbotName);
                return new CrmRoutingResult(504, "application/json",
                    """{"error":"Gateway timeout — chatbot did not respond in time."}""");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[CRM] Could not reach {ChatbotName}", request.ChatbotName);
                return new CrmRoutingResult(502, "application/json",
                    """{"error":"Bad gateway — could not reach chatbot service."}""");
            }
        }

        private static CrmRoutingResult BadRequest(string message) =>
            new(400, "application/json", JsonSerializer.Serialize(new { error = message }));
    }
}