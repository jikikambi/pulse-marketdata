using MarketData.Adapter.Api;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureOpenTelemetry();

// --- API / SWAGGER ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new OpenApiInfo { Title = "Clients API", Version = "v1" });
});

builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

// --- CORS ---
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CORS_POLICY, policy =>
    {
        policy.WithOrigins(corsOrigins ?? [])
        .SetIsOriginAllowed(origin => true)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// --- SIGNALR ---
builder.Services.RegisterSignalR();

var app = builder.Build();

// --- DEV ONLY ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --- PIPELINE ---
app.UseHttpsRedirection();
app.UseCors(CORS_POLICY);

app.MapDefaultEndpoints();

app.MapMinimalApis();
//app.MapHub<SignalPulseHub>("/hubs/signalpulse");

await app.RunAsync();

public partial class Program
{
    public const string CORS_POLICY = "Frontend";
}