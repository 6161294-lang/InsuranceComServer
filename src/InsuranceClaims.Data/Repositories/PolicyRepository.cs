using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaims.Data.Repositories;

public class PolicyRepository : IPolicyRepository
{
    private readonly AppDbContext _context;

    public PolicyRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<Policy> Items, int TotalCount)> GetPagedAsync(PolicyQueryParameters query, CancellationToken cancellationToken = default)
    {
        IQueryable<Policy> policies = _context.Policies.AsNoTracking();

        if (query.InsuranceTypeId is int typeId)
        {
            policies = policies.Where(p => p.InsuranceTypeId == typeId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            policies = policies.Where(p => EF.Functions.ILike(p.PolicyNumber, pattern) || EF.Functions.ILike(p.HolderName, pattern));
        }

        var totalCount = await policies.CountAsync(cancellationToken);

        // Paging is translated to SQL (LIMIT/OFFSET) - nothing is materialized before it.
        var items = await policies
            .Include(p => p.InsuranceType)
            .OrderBy(p => p.PolicyNumber)
            .ThenBy(p => p.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<Policy?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Policies
            .AsNoTracking()
            .Include(p => p.InsuranceType)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Policy?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Policies
            .Include(p => p.InsuranceType)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> PolicyNumberExistsAsync(string policyNumber, CancellationToken cancellationToken = default) =>
        _context.Policies.AnyAsync(p => p.PolicyNumber == policyNumber, cancellationToken);

    public Task<bool> HasClaimsAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Claims.AnyAsync(c => c.PolicyId == id, cancellationToken);

    public void Add(Policy policy) => _context.Policies.Add(policy);

    public void Remove(Policy policy) => _context.Policies.Remove(policy);
}
