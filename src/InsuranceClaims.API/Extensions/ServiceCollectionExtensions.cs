using System.Reflection;
using System.Text;
using InsuranceClaims.API.Models;
using InsuranceClaims.API.Security;
using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace InsuranceClaims.API.Extensions;

public static class ServiceCollectionExtensions
{
    private const string BearerSchemeId = "Bearer";

    /// <summary>
    /// JWT bearer authentication. Settings come from IOptions&lt;JwtSettings&gt; (section "Jwt");
    /// the signing key is kept in User Secrets and validated at startup.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .Validate(s => !string.IsNullOrWhiteSpace(s.Issuer) && !string.IsNullOrWhiteSpace(s.Audience),
                "Jwt:Issuer and Jwt:Audience must be configured.")
            .Validate(s => Encoding.UTF8.GetByteCount(s.Key ?? string.Empty) >= 32,
                "Jwt:Key must be at least 32 bytes. Store it in User Secrets: dotnet user-secrets set \"Jwt:Key\" \"<long random string>\" --project src/InsuranceClaims.API")
            .Validate(s => s.ExpiryMinutes > 0, "Jwt:ExpiryMinutes must be positive.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                options.MapInboundClaims = false; // keep the short claim names ("sub", "role", ...)
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = AppClaimTypes.Name,
                    RoleClaimType = AppClaimTypes.Role
                };

                // 401/403 use the same JSON error shape as the rest of the API.
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new ErrorResponse
                        {
                            Status = StatusCodes.Status401Unauthorized,
                            Message = "נדרשת התחברות",
                            CorrelationId = context.HttpContext.TraceIdentifier
                        });
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new ErrorResponse
                        {
                            Status = StatusCodes.Status403Forbidden,
                            Message = "אין לך הרשאה לבצע פעולה זו",
                            CorrelationId = context.HttpContext.TraceIdentifier
                        });
                    }
                };
            });

        return services;
    }

    /// <summary>Swagger with XML comments and an "Authorize" button for pasting a JWT.</summary>
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Insurance Claims API",
                Version = "v1",
                Description = "ניהול תביעות ביטוח - כולל טיפול בתחרות על תביעה בין שמאים"
            });

            options.AddSecurityDefinition(BearerSchemeId, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Description = "Paste the token returned by POST /api/auth/login (without the word Bearer)."
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerSchemeId, document)] = new List<string>()
            });

            foreach (var assembly in new[] { Assembly.GetExecutingAssembly(), typeof(ClaimDto).Assembly })
            {
                var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            }
        });

        return services;
    }
}
