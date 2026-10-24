using System.Text.Json.Serialization;
using InsuranceClaims.API.Extensions;
using InsuranceClaims.API.Middleware;
using InsuranceClaims.API.Models;
using InsuranceClaims.API.Security;
using InsuranceClaims.Core.Interfaces.Services;
using InsuranceClaims.Data;
using InsuranceClaims.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;

var bootstrapLogger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Hosting platforms (Render, Docker) tell the app which port to listen on through PORT.
    if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } port)
    {
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
    }

    // ---------- Logging: NLog behind ILogger (levels come from the "Logging" section) ----------
    builder.Logging.ClearProviders();
    builder.Host.UseNLog(new NLogAspNetCoreOptions { RemoveLoggerFactoryFilter = false });

    // Fail fast on lifetime mistakes, e.g. a Singleton that captures a Scoped service.
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });

    // ---------- Configuration ----------
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Connection string 'DefaultConnection' is missing. Locally store it in User Secrets: " +
            "dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Host=localhost;Port=5432;Database=insurance_claims;Username=...;Password=...\" --project src/InsuranceClaims.API " +
            "(on a server use the ConnectionStrings__DefaultConnection environment variable).");
    }

    // ---------- Services ----------
    // Lifetimes:
    //  * DbContext, repositories, unit of work and business services are Scoped - one instance per HTTP request,
    //    so every repository in a request shares the same DbContext (one unit of work), and a DbContext
    //    (which is not thread safe) is never shared between concurrent requests.
    //  * A Singleton that holds a Scoped service is a bug ("captive dependency"): the Singleton lives for the
    //    whole application, so it would keep the first request's DbContext forever - sharing it across threads,
    //    serving stale tracked data and using it after it was disposed. ValidateScopes above throws on this.
    //  * Stateless helpers (password hasher, token generator, TimeProvider) are Singletons.
    builder.Services.AddDataLayer(connectionString.ToNpgsqlConnectionString());
    builder.Services.AddServiceLayer(builder.Configuration["AutoMapper:LicenseKey"]);
    builder.Services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();

    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddAuthorization();

    // The React client is a separate site, so its origin(s) must be allowed: comma-separated in Cors:AllowedOrigins
    // (environment variable Cors__AllowedOrigins). Empty means no cross-origin access (the dev server proxies /api).
    var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(origin => origin.TrimEnd('/'))
        .ToArray();
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

    builder.Services
        .AddControllers()
        .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
        .ConfigureApiBehaviorOptions(options =>
        {
            // Model validation errors use the same JSON shape as every other error.
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(entry => entry.Value is { Errors.Count: > 0 })
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors.Select(error => string.IsNullOrEmpty(error.ErrorMessage) ? "Invalid value." : error.ErrorMessage).ToArray());

                return new BadRequestObjectResult(new ErrorResponse
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "הבקשה אינה תקינה",
                    CorrelationId = context.HttpContext.TraceIdentifier,
                    Errors = errors
                });
            };
        });

    builder.Services.AddSwaggerWithJwt();
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // ---------- Database: apply migrations (and with them the seed data) on startup ----------
    if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    // ---------- Pipeline (order matters) ----------
    app.UseCors();                                    // 0. first, so preflight requests and error responses carry CORS headers
    app.UseMiddleware<ExceptionHandlingMiddleware>(); // 1. outermost: catches everything below
    app.UseMiddleware<CorrelationIdMiddleware>();     // 2. id + logging scope for the rest of the request
    app.UseMiddleware<RequestLoggingMiddleware>();    // 3. one log line per request, with the id

    // API docs: Development, or explicitly enabled with Swagger__Enabled=true (off by default in Production).
    if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseAuthentication(); // who are you - before -
    app.UseAuthorization();  // - what are you allowed to do

    app.MapControllers();
    app.MapHealthChecks("/health");

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException is thrown on purpose by "dotnet ef" tooling and is not an error.
    bootstrapLogger.Error(ex, "Application stopped because of an exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}
