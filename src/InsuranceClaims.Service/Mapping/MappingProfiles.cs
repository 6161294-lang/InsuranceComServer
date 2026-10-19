using AutoMapper;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;

namespace InsuranceClaims.Service.Mapping;

// Request DTO -> entity maps are validated against the source (MemberList.Source):
// every request field must land somewhere, while ids, navigations and server-controlled
// fields (status, handler, version, password hash) are never taken from the client.

public class InsuranceTypeProfile : Profile
{
    public InsuranceTypeProfile()
    {
        CreateMap<InsuranceTypeCreateDto, InsuranceType>(MemberList.Source);
        CreateMap<InsuranceTypeUpdateDto, InsuranceType>(MemberList.Source);
        CreateMap<InsuranceType, InsuranceTypeDto>();
    }
}

public class PolicyProfile : Profile
{
    public PolicyProfile()
    {
        CreateMap<PolicyCreateDto, Policy>(MemberList.Source)
            .ForMember(d => d.StartDate, o => o.MapFrom(s => DateTimeUtc.ToUtc(s.StartDate!.Value)))
            .ForMember(d => d.EndDate, o => o.MapFrom(s => DateTimeUtc.ToUtc(s.EndDate!.Value)))
            .ForSourceMember(s => s.StartDate, o => o.DoNotValidate()) // used by the MapFrom above
            .ForSourceMember(s => s.EndDate, o => o.DoNotValidate());

        CreateMap<PolicyUpdateDto, Policy>(MemberList.Source)
            .ForMember(d => d.StartDate, o => o.MapFrom(s => DateTimeUtc.ToUtc(s.StartDate!.Value)))
            .ForMember(d => d.EndDate, o => o.MapFrom(s => DateTimeUtc.ToUtc(s.EndDate!.Value)))
            .ForSourceMember(s => s.StartDate, o => o.DoNotValidate()) // used by the MapFrom above
            .ForSourceMember(s => s.EndDate, o => o.DoNotValidate());

        CreateMap<Policy, PolicyDto>()
            .ForMember(d => d.InsuranceTypeName, o => o.MapFrom(s => s.InsuranceType!.Name));
    }
}

public class ClaimProfile : Profile
{
    public ClaimProfile()
    {
        CreateMap<ClaimCreateDto, Claim>(MemberList.Source);

        CreateMap<ClaimUpdateDto, Claim>(MemberList.Source)
            .ForSourceMember(s => s.Version, o => o.DoNotValidate())
            .ForMember(d => d.Version, o => o.Ignore());

        CreateMap<Claim, ClaimDto>()
            .ForMember(d => d.PolicyNumber, o => o.MapFrom(s => s.Policy!.PolicyNumber))
            .ForMember(d => d.InsuranceTypeId, o => o.MapFrom(s => s.Policy!.InsuranceTypeId))
            .ForMember(d => d.InsuranceTypeName, o => o.MapFrom(s => s.Policy!.InsuranceType!.Name))
            .ForMember(d => d.HandlerName, o => o.MapFrom(s => s.Handler!.FullName));
    }
}

public class EmployeeProfile : Profile
{
    public EmployeeProfile()
    {
        // The plain password is hashed by the service and never mapped to the entity.
        CreateMap<RegisterDto, Employee>(MemberList.Source)
            .ForSourceMember(s => s.Password, o => o.DoNotValidate())
            .ForSourceMember(s => s.SpecializationIds, o => o.DoNotValidate());

        CreateMap<EmployeeUpdateDto, Employee>(MemberList.Source)
            .ForSourceMember(s => s.SpecializationIds, o => o.DoNotValidate());

        // EmployeeDto has no password field - the hash never leaves the server.
        CreateMap<Employee, EmployeeDto>();
    }
}

internal static class DateTimeUtc
{
    /// <summary>PostgreSQL timestamptz accepts only UTC values; dates without a zone are treated as UTC.</summary>
    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
