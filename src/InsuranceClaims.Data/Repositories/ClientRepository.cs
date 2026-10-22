using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Enums;
using InsuranceClaims.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaims.Data.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly AppDbContext _context;

    public ClientRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<Client> Items, int TotalCount)> GetPagedAsync(PagingParameters paging, int? appraiserId, CancellationToken cancellationToken = default)
    {
        IQueryable<Client> clients = _context.Clients.AsNoTracking();

        if (appraiserId is int id)
        {
            clients = clients.Where(c => c.Claims.Any(claim =>
                claim.Status == ClaimStatus.InTreatment &&
                (claim.HandlerId == id || claim.Referrals.Any(r => r.AppraiserId == id))));
        }

        var totalCount = await clients.CountAsync(cancellationToken);

        var items = await clients
            .OrderBy(c => c.FullName)
            .ThenBy(c => c.Id)
            .Include(c => c.InsuranceTypes)
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<Client?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Clients
            .AsNoTracking()
            .Include(c => c.InsuranceTypes)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Client?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Clients
            .Include(c => c.InsuranceTypes)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Client?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _context.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Email == email, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        _context.Clients.AnyAsync(c => c.Email == email, cancellationToken);

    public Task<bool> NationalIdExistsAsync(string nationalId, CancellationToken cancellationToken = default) =>
        _context.Clients.AnyAsync(c => c.NationalId == nationalId, cancellationToken);

    public Task<bool> HasActiveRelationWithAppraiserAsync(int clientId, int appraiserId, CancellationToken cancellationToken = default) =>
        _context.Claims.AnyAsync(claim =>
            claim.ClientId == clientId &&
            claim.Status == ClaimStatus.InTreatment &&
            (claim.HandlerId == appraiserId || claim.Referrals.Any(r => r.AppraiserId == appraiserId)),
            cancellationToken);

    public Task<bool> HoldsInsuranceTypeAsync(int clientId, int insuranceTypeId, CancellationToken cancellationToken = default) =>
        _context.Clients.AnyAsync(c => c.Id == clientId && c.InsuranceTypes.Any(t => t.Id == insuranceTypeId), cancellationToken);

    public void Add(Client client) => _context.Clients.Add(client);
}
