using AutoMapper;
using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Interfaces.Repositories;
using InsuranceClaims.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace InsuranceClaims.Service.Services;

public class ClientService : IClientService
{
    private const string ClientNotFound = "הלקוח לא נמצא";

    private readonly IClientRepository _clients;
    private readonly IInsuranceTypeRepository _insuranceTypes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ClientService> _logger;

    public ClientService(
        IClientRepository clients,
        IInsuranceTypeRepository insuranceTypes,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ClientService> logger)
    {
        _clients = clients;
        _insuranceTypes = insuranceTypes;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<OperationResult<PagedResult<ClientDto>>> GetPagedAsync(CurrentUser user, PagingParameters paging, CancellationToken cancellationToken = default)
    {
        if (!user.IsManager && !user.IsAppraiser)
        {
            return OperationResult<PagedResult<ClientDto>>.Forbidden("אין הרשאה לצפות ברשימת הלקוחות");
        }

        // An appraiser only gets the clients they have an active claim or referral for.
        var (items, totalCount) = await _clients.GetPagedAsync(paging, user.IsAppraiser ? user.Id : null, cancellationToken);
        var dtos = _mapper.Map<List<ClientDto>>(items);
        if (user.IsAppraiser)
        {
            dtos.ForEach(HideSensitiveData);
        }

        return OperationResult<PagedResult<ClientDto>>.Success(new PagedResult<ClientDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = paging.Page,
            PageSize = paging.PageSize
        });
    }

    public async Task<OperationResult<ClientDto>> GetByIdAsync(CurrentUser user, int id, CancellationToken cancellationToken = default)
    {
        var client = await _clients.GetByIdAsync(id, cancellationToken);
        if (client is null)
        {
            return OperationResult<ClientDto>.NotFound(ClientNotFound);
        }

        var dto = _mapper.Map<ClientDto>(client);

        if (user.IsManager || (user.IsClient && user.Id == id))
        {
            return OperationResult<ClientDto>.Success(dto);
        }

        if (user.IsAppraiser && await _clients.HasActiveRelationWithAppraiserAsync(id, user.Id, cancellationToken))
        {
            HideSensitiveData(dto);
            return OperationResult<ClientDto>.Success(dto);
        }

        return OperationResult<ClientDto>.Forbidden("אין הרשאה לצפות בפרטי הלקוח");
    }

    public async Task<OperationResult<ClientDto>> UpdateAsync(CurrentUser user, int id, ClientUpdateDto dto, CancellationToken cancellationToken = default)
    {
        if (!user.IsClient || user.Id != id)
        {
            return OperationResult<ClientDto>.Forbidden("לקוח רשאי לעדכן רק את הפרטים של עצמו");
        }

        var client = await _clients.GetForUpdateAsync(id, cancellationToken);
        if (client is null)
        {
            return OperationResult<ClientDto>.NotFound(ClientNotFound);
        }

        _mapper.Map(dto, client);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Client {ClientId} updated their details", id);
        return OperationResult<ClientDto>.Success(_mapper.Map<ClientDto>(client));
    }

    public async Task<OperationResult<ClientDto>> UpdateInsuranceTypesAsync(CurrentUser user, int id, ClientInsuranceTypesUpdateDto dto, CancellationToken cancellationToken = default)
    {
        if (!user.IsManager)
        {
            return OperationResult<ClientDto>.Forbidden("רק מנהל רשאי לעדכן את הביטוחים של לקוח");
        }

        var client = await _clients.GetForUpdateAsync(id, cancellationToken);
        if (client is null)
        {
            return OperationResult<ClientDto>.NotFound(ClientNotFound);
        }

        var insuranceTypes = await _insuranceTypes.GetByIdsAsync(dto.InsuranceTypeIds, cancellationToken);
        if (insuranceTypes.Count != dto.InsuranceTypeIds.Distinct().Count())
        {
            return OperationResult<ClientDto>.ValidationFailed("אחד או יותר מסוגי הביטוח לא נמצאו");
        }

        client.InsuranceTypes.Clear();
        foreach (var insuranceType in insuranceTypes)
        {
            client.InsuranceTypes.Add(insuranceType);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Insurance types of client {ClientId} updated by manager {ManagerId}", id, user.Id);
        return OperationResult<ClientDto>.Success(_mapper.Map<ClientDto>(client));
    }

    /// <summary>Appraisers receive contact details only - never the national id.</summary>
    private static void HideSensitiveData(ClientDto dto) => dto.NationalId = null;
}
