using AutoMapper;
using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;

namespace InsuranceClaims.Service.Mapping;

// Request DTO -> entity maps are validated against the source (MemberList.Source):
// every request field must land somewhere, while ids, navigations and server-controlled
// fields (status, handler, version, password hash) are never taken from the client.
// No entity is ever returned by the API - only the response DTOs below, which have no password field.

public class InsuranceTypeProfile : Profile
{
    public InsuranceTypeProfile()
    {
        CreateMap<InsuranceTypeCreateDto, InsuranceType>(MemberList.Source);
        CreateMap<InsuranceTypeUpdateDto, InsuranceType>(MemberList.Source);
        CreateMap<InsuranceType, InsuranceTypeDto>();
    }
}

public class ClientProfile : Profile
{
    public ClientProfile()
    {
        // The plain password is hashed by the service and never mapped to the entity.
        CreateMap<RegisterClientDto, Client>(MemberList.Source)
            .ForSourceMember(s => s.Password, o => o.DoNotValidate())
            .ForSourceMember(s => s.InsuranceTypeIds, o => o.DoNotValidate());

        CreateMap<ClientUpdateDto, Client>(MemberList.Source);

        // ClientDto has no password field - the hash never leaves the server.
        CreateMap<Client, ClientDto>();
    }
}

public class EmployeeProfile : Profile
{
    public EmployeeProfile()
    {
        CreateMap<EmployeeCreateDto, Employee>(MemberList.Source)
            .ForSourceMember(s => s.Password, o => o.DoNotValidate())
            .ForSourceMember(s => s.SpecializationIds, o => o.DoNotValidate())
            .ForSourceMember(s => s.Role, o => o.DoNotValidate()) // set explicitly by the service
            .ForMember(d => d.Role, o => o.Ignore());

        CreateMap<Employee, EmployeeDto>();
    }
}

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<Employee, UserDto>()
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.ToString()));

        CreateMap<Client, UserDto>()
            .ForMember(d => d.Role, o => o.MapFrom(_ => AppRoles.Client));
    }
}

public class ClaimProfile : Profile
{
    public ClaimProfile()
    {
        CreateMap<ClaimCreateDto, Claim>(MemberList.Source)
            .ForSourceMember(s => s.SaveAsDraft, o => o.DoNotValidate()); // decides the status, set by the service

        CreateMap<ClaimUpdateDto, Claim>(MemberList.Source)
            .ForSourceMember(s => s.Version, o => o.DoNotValidate())
            .ForMember(d => d.Version, o => o.Ignore());

        CreateMap<Claim, ClaimDto>()
            .ForMember(d => d.ClientName, o => o.MapFrom(s => s.Client!.FullName))
            .ForMember(d => d.InsuranceTypeName, o => o.MapFrom(s => s.InsuranceType!.Name))
            .ForMember(d => d.HandlerName, o => o.MapFrom(s => s.Handler!.FullName));
    }
}

public class ReferralProfile : Profile
{
    public ReferralProfile()
    {
        CreateMap<ReferralUpdateDto, Referral>(MemberList.Source);

        CreateMap<Referral, ReferralDto>()
            .ForMember(d => d.ClaimNumber, o => o.MapFrom(s => s.Claim!.ClaimNumber))
            .ForMember(d => d.ClaimStatus, o => o.MapFrom(s => s.Claim!.Status))
            .ForMember(d => d.ClaimDescription, o => o.MapFrom(s => s.Claim!.Description))
            .ForMember(d => d.ClaimAmount, o => o.MapFrom(s => s.Claim!.Amount))
            .ForMember(d => d.ClientId, o => o.MapFrom(s => s.Claim!.ClientId))
            .ForMember(d => d.ClientName, o => o.MapFrom(s => s.Claim!.Client!.FullName))
            .ForMember(d => d.InsuranceTypeName, o => o.MapFrom(s => s.Claim!.InsuranceType!.Name))
            .ForMember(d => d.AppraiserName, o => o.MapFrom(s => s.Appraiser!.FullName));
    }
}
