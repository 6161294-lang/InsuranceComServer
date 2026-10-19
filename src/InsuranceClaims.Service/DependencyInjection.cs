using InsuranceClaims.Core.Interfaces.Services;
using InsuranceClaims.Service.Mapping;
using InsuranceClaims.Service.Security;
using InsuranceClaims.Service.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InsuranceClaims.Service;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the business services (Scoped - they depend on Scoped repositories),
    /// the stateless password hasher (Singleton) and the AutoMapper profiles.
    /// </summary>
    public static IServiceCollection AddServiceLayer(this IServiceCollection services, string? autoMapperLicenseKey = null)
    {
        services.AddAutoMapper(cfg =>
        {
            if (!string.IsNullOrWhiteSpace(autoMapperLicenseKey))
            {
                cfg.LicenseKey = autoMapperLicenseKey;
            }
        }, typeof(ClaimProfile).Assembly);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        services.AddScoped<IInsuranceTypeService, InsuranceTypeService>();
        services.AddScoped<IPolicyService, PolicyService>();
        services.AddScoped<IClaimService, ClaimService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
