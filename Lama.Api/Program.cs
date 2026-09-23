using Lama.Api.Auth;
using Lama.Api.Integrations.TelegramBot;
using Lama.Application;
using Lama.Infrastructure;
using Lama.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Paste the token from POST /api/auth/login",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer")] = new List<string>()
    });
});

// CORS: in production list the CRM origins in Cors:AllowedOrigins
// (e.g. "https://crm.iconicu.kz"); with none configured any localhost is allowed for development.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("CrmFrontend", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins);
        else
            policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost");

        policy.AllowAnyMethod().AllowAnyHeader().AllowCredentials();
    });
});

// Behind nginx: keep the real scheme and client IP
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // The proxy runs on the same host and is not in a known network by default
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add Application and Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// IconicU Telegram bot intake (POST /api/integrations/telegram/leads)
builder.Services.Configure<TelegramBotOptions>(
    builder.Configuration.GetSection(TelegramBotOptions.SectionName));

// --- Sign-in with a JWT issued by POST /api/auth/login -----------------------
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<JwtTokenService>();

builder.Services.ConfigureOptions<ConfigureJwtBearerOptions>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Everything needs a signed-in user unless it opts out with [AllowAnonymous]
// (sign-in itself, and the bot intake, which is guarded by its own API key).
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

app.UseForwardedHeaders();

// On the server the release runs migrations itself (Database:MigrateOnStartup=true),
// so a deploy never leaves the schema behind the code.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Lama CRM API v1");
        options.RoutePrefix = "swagger";
    });
}

// Use CORS
app.UseCors("CrmFrontend");

// Disable HTTPS redirection in development to avoid CORS issues
// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Liveness probe for systemd/monitoring: no database, no auth
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.Run();
