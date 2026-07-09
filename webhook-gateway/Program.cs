using Microsoft.AspNetCore.Server.Kestrel.Core;
using webhook_gateway.Options;
using webhook_gateway.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Raise the request body size limit to 50 MB.
// 360dialog webhooks are always JSON (never raw binary), so the default 30 MB
// is fine in practice — but this gives headroom for unexpectedly large payloads.
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50 MB
});

// Named HttpClients — one per downstream chatbot
builder.Services.AddHttpClient(ForwardingService.UaeChatbotClient, client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Downstream:UaeChatbot:BaseUrl"] ?? "http://localhost:8041");

    client.Timeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue("Downstream:TimeoutSeconds", 55));  // must be > 360dialog retry timeout (~20s)
});
builder.Services.AddHttpClient(ForwardingService.MalaysiaChatbotClient, client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Downstream:MalaysiaChatbot:BaseUrl"] ?? "http://localhost:8043");

    client.Timeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue("Downstream:TimeoutSeconds", 55));  // must be > 360dialog retry timeout (~20s)
});


builder.Services.AddHttpClient(ForwardingService.SalesSupportClient, client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Downstream:SalesSupport:BaseUrl"] ?? "http://localhost:8042");

    client.Timeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue("Downstream:TimeoutSeconds", 55));  // must be > 360dialog retry timeout (~20s)
});


// CRM status-callback client — forwards to UAE Chatbot backend /api/crm/ticket-status
builder.Services.AddHttpClient(ForwardingService.KsaChatbotClient, client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Downstream:KsaChatbot:BaseUrl"] ?? "http://localhost:8044");

    client.Timeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue("Downstream:TimeoutSeconds", 55));  // must be > 360dialog retry timeout (~20s)
});


// CRM status-callback client — forwards to UAE Chatbot backend /api/crm/ticket-status
builder.Services.AddHttpClient(ForwardingService.CrmCallbackClient, client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Downstream:CrmCallback:BaseUrl"] ?? "http://localhost:8041");

    client.Timeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue("Downstream:TimeoutSeconds", 55));
});

// Singleton — shares dedup cache across all requests
builder.Services.AddSingleton<ForwardingService>();

// ── CRM → Chatbot shop-assignment routing ───────────────────────────────────
// Bind "ChatbotRouting" config section directly to a Dictionary<string, ChatbotRoute>.
// Adding a new chatbot is just a new entry in appsettings.json — no code change.
builder.Services.Configure<Dictionary<string, ChatbotRoute>>(
    builder.Configuration.GetSection("ChatbotRouting"));

// Single HttpClient for all CRM shop-assignment forwarding — target URL is
// built dynamically per request from ChatbotRouting config.
builder.Services.AddHttpClient(CrmRoutingService.HttpClientName, client =>
{
    client.Timeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue("Downstream:TimeoutSeconds", 30));
});

builder.Services.AddScoped<ICrmRoutingService, CrmRoutingService>();

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────


app.UseSwagger();
app.UseSwaggerUI();


// REMOVED app.UseHttpsRedirection()
// Gateway runs behind IIS which handles HTTPS termination.
// UseHttpsRedirection causes 301 redirects → 360dialog POSTs twice → double bot reply.
app.UseStaticFiles();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

app.Run();