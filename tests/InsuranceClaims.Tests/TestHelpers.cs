using AutoMapper;
using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Enums;
using InsuranceClaims.Service.Mapping;
using Microsoft.Extensions.Logging.Abstractions;

namespace InsuranceClaims.Tests;

internal static class TestMapper
{
    public static MapperConfiguration CreateConfiguration() =>
        new(cfg => cfg.AddMaps(typeof(ClaimProfile).Assembly), NullLoggerFactory.Instance);

    public static IMapper Create() => CreateConfiguration().CreateMapper();
}

internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _utcNow;

    public FixedTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;
}

/// <summary>Callers and ids shared by the service tests.</summary>
internal static class TestData
{
    public const int CarTypeId = 1;
    public const int HomeTypeId = 2;

    public const int ClientId = 10;
    public const int OtherClientId = 11;
    public const int ManagerId = 1;
    public const int AppraiserId = 2;
    public const int OtherAppraiserId = 3;

    public static readonly CurrentUser ClientUser = new(ClientId, AppRoles.Client);
    public static readonly CurrentUser OtherClientUser = new(OtherClientId, AppRoles.Client);
    public static readonly CurrentUser ManagerUser = new(ManagerId, AppRoles.Manager);
    public static readonly CurrentUser AppraiserUser = new(AppraiserId, AppRoles.Appraiser);
    public static readonly CurrentUser OtherAppraiserUser = new(OtherAppraiserId, AppRoles.Appraiser);

    public static InsuranceType CarType() => new() { Id = CarTypeId, Name = "ביטוח רכב" };
    public static InsuranceType HomeType() => new() { Id = HomeTypeId, Name = "ביטוח דירה" };

    public static Client NewClient(int id = ClientId, params InsuranceType[] held)
    {
        var client = new Client
        {
            Id = id,
            FullName = "לקוח לדוגמה",
            Email = "client@example.com",
            PasswordHash = "hash",
            Phone = "0501234567",
            Address = "רחוב 1",
            NationalId = "123456789"
        };
        foreach (var type in held)
        {
            client.InsuranceTypes.Add(type);
        }

        return client;
    }

    public static Employee NewAppraiser(int id, params InsuranceType[] specializations)
    {
        var appraiser = new Employee { Id = id, FullName = $"שמאי {id}", Email = $"appraiser{id}@example.com", PasswordHash = "hash", Role = EmployeeRole.Appraiser };
        foreach (var type in specializations)
        {
            appraiser.Specializations.Add(type);
        }

        return appraiser;
    }

    public static Claim NewClaim(Guid version, ClaimStatus status = ClaimStatus.Opened, int clientId = ClientId, int? handlerId = null) => new()
    {
        Id = 100,
        ClaimNumber = "CLM-0100",
        ClientId = clientId,
        Client = NewClient(clientId),
        InsuranceTypeId = CarTypeId,
        InsuranceType = CarType(),
        Description = "תאונה",
        Amount = 5000m,
        Status = status,
        HandlerId = handlerId,
        OpenedAt = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc),
        Version = version
    };
}
