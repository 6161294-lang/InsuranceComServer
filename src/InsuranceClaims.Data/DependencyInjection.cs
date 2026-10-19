using InsuranceClaims.Core.Interfaces.Repositories;
using InsuranceClaims.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InsuranceClaims.Data;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the DbContext (Npgsql) and the repositories.
    /// All of them are Scoped: one DbContext per HTTP request, shared by every repository
    /// in that request so they form a single unit of work.
    /// </summary>
    public static IServiceCollection AddDataLayer(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IInsuranceTypeRepository, InsuranceTypeRepository>();
        services.AddScoped<IPolicyRepository, PolicyRepository>();
        services.AddScoped<IClaimRepository, ClaimRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();

        return services;
    }
}
