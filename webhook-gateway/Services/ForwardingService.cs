using System.Net.Http.Headers;

namespace webhook_gateway.Services
{
    /// <summary>
    /// Reads the incoming raw request body and forwards it — byte-for-byte, unmodified —
    /// to the correct downstream chatbot, then returns the response back to 360dialog.
    ///
    /// 360dialog always sends JSON webhooks for ALL message types (text, image, voice,
    /// document, location, etc.). The actual binary file is never in the webhook payload —
    /// only a media_id is included. The downstream chatbot downloads the file separately.
    /// So this gateway only ever forwards JSON bodies.
    /// </summary>
    public class ForwardingService
    {
        public const string UaeChatbotClient   = "UaeChatbotClient";
        public const string SalesSupportClient = "SalesSupportClient";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ForwardingService> _logger;

        public ForwardingService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ForwardingService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration     = configuration;
            _logger            = logger;
        }

        // ── UAE Chatbot ──────────────────────────────────────────────────────
        public Task<ForwardResult> ForwardToUaeAsync(
            HttpRequest request, CancellationToken ct = default)
        {
            var path = _configuration["Downstream:UaeChatbot:WebhookPath"]
                       ?? "/webhook/whatsapp-webhook";

            return ForwardAsync(UaeChatbotClient, request, path, ct);
        }

        // ── Sales Support Chatbot ────────────────────────────────────────────
        public Task<ForwardResult> ForwardToSalesSupportAsync(
            HttpRequest request, CancellationToken ct = default)
        {
            var path = _configuration["Downstream:SalesSupport:WebhookPath"]
                       ?? "/webhook/whatsapp-webhook";

            return ForwardAsync(SalesSupportClient, request, path, ct);
        }

        // ── Internal proxy engine ────────────────────────────────────────────
        private async Task<ForwardResult> ForwardAsync(
            string clientName,
            HttpRequest incoming,
            string downstreamPath,
            CancellationToken ct)
        {
            // ── 1. Append query string ────────────────────────────────────────
            // IMPORTANT: Must be forwarded for GET verification.
            // 360dialog sends: ?hub.mode=subscribe&hub.challenge=xxx&hub.verify_token=xxx
            // Without this the downstream chatbot never sees the challenge and fails.
            var fullDownstreamPath = incoming.QueryString.HasValue
                ? downstreamPath + incoming.QueryString.Value
                : downstreamPath;

            // ── 2. Read raw body bytes ────────────────────────────────────────
            // EnableBuffering allows multiple reads safely
            incoming.EnableBuffering();
            using var ms = new MemoryStream();
            await incoming.Body.CopyToAsync(ms, ct);
            var bodyBytes = ms.ToArray();
            incoming.Body.Seek(0, SeekOrigin.Begin);

            // ── 3. Build outgoing request ─────────────────────────────────────
            var outgoing = new HttpRequestMessage
            {
                Method     = new HttpMethod(incoming.Method),
                RequestUri = new Uri(fullDownstreamPath, UriKind.Relative),
                Content    = new ByteArrayContent(bodyBytes)
            };

            // Preserve content-type exactly (e.g. application/json; charset=utf-8)
            if (!string.IsNullOrEmpty(incoming.ContentType))
                outgoing.Content.Headers.ContentType =
                    MediaTypeHeaderValue.Parse(incoming.ContentType);

            // ── 4. Forward 360dialog signature headers ────────────────────────
            // D360-Signature         — 360dialog's own HMAC signature header
            // X-Hub-Signature        — Meta/WhatsApp legacy variant
            // X-Hub-Signature-256    — Meta/WhatsApp SHA-256 variant
            // Downstream chatbots use these to verify the payload is genuine.
            foreach (var key in _headersToForward)
            {
                if (incoming.Headers.TryGetValue(key, out var val))
                    outgoing.Headers.TryAddWithoutValidation(key, val.ToArray());
            }

            _logger.LogInformation(
                "[{Client}] → {Method} {Path} ({Bytes} bytes)",
                clientName, incoming.Method, fullDownstreamPath, bodyBytes.Length);

            // ── 5. Send and return ────────────────────────────────────────────
            var httpClient = _httpClientFactory.CreateClient(clientName);

            try
            {
                var response = await httpClient.SendAsync(
                    outgoing, HttpCompletionOption.ResponseContentRead, ct);

                // Downstream chatbots always return JSON so reading as string is safe
                var body        = await response.Content.ReadAsStringAsync(ct);
                var contentType = response.Content.Headers.ContentType?.ToString()
                                  ?? "application/json";

                _logger.LogInformation(
                    "[{Client}] ← {StatusCode}", clientName, (int)response.StatusCode);

                return new ForwardResult((int)response.StatusCode, contentType, body);
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("[{Client}] Downstream timed out", clientName);
                return new ForwardResult(504, "application/json",
                    """{"error":"Gateway timeout — downstream service did not respond in time."}""");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "[{Client}] Could not reach downstream service", clientName);
                return new ForwardResult(502, "application/json",
                    """{"error":"Bad gateway — could not reach downstream service."}""");
            }
        }

        // Headers sent by 360dialog that the downstream chatbots need to verify authenticity
        private static readonly string[] _headersToForward =
        [
            "D360-Signature",        // 360dialog's own HMAC signature header
            "X-Hub-Signature",       // Meta/WhatsApp legacy signature
            "X-Hub-Signature-256",   // Meta/WhatsApp SHA-256 signature
        ];
    }

    /// <summary>Carries the downstream response back to the controller.</summary>
    public record ForwardResult(int StatusCode, string ContentType, string Body);
}
