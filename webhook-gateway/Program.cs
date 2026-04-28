using Microsoft.AspNetCore.Server.Kestrel.Core;
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
        builder.Configuration.GetValue("Downstream:TimeoutSeconds", 30));
});

builder.Services.AddHttpClient(ForwardingService.SalesSupportClient, client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Downstream:SalesSupport:BaseUrl"] ?? "http://localhost:8042");

    client.Timeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue("Downstream:TimeoutSeconds", 30));
});

builder.Services.AddScoped<ForwardingService>();

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

app.Run();
