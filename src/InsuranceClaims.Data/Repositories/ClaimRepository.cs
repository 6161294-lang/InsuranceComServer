using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaims.Data.Repositories;

public class ClaimRepository : IClaimRepository
{
    private readonly AppDbContext _context;

    public ClaimRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<Claim> Items, int TotalCount)> GetPagedAsync(ClaimQueryParameters query, CancellationToken cancellationToken = default)
    {
        IQueryable<Claim> claims = _context.Claims.AsNoTracking();

        if (query.Status is { } status)
        {
            claims = claims.Where(c => c.Status == status);
        }

        if (query.InsuranceTypeId is int typeId)
        {
            claims = claims.Where(c => c.Policy!.InsuranceTypeId == typeId);
        }

        if (query.HandlerId is int handlerId)
        {
            claims = claims.Where(c => c.HandlerId == handlerId);
        }

        var totalCount = await claims.CountAsync(cancellationToken);

        IOrderedQueryable<Claim> ordered = (query.SortBy, query.Descending) switch
        {
            (ClaimSortField.Amount, false) => claims.OrderBy(c => c.Amount),
            (ClaimSortField.Amount, true) => claims.OrderByDescending(c => c.Amount),
            (ClaimSortField.Status, false) => claims.OrderBy(c => c.Status),
            (ClaimSortField.Status, true) => claims.OrderByDescending(c => c.Status),
            (ClaimSortField.Title, false) => claims.OrderBy(c => c.Title),
            (ClaimSortField.Title, true) => claims.OrderByDescending(c => c.Title),
            (_, false) => claims.OrderBy(c => c.CreatedAt),
            _ => claims.OrderByDescending(c => c.CreatedAt)
        };

        // Related data is loaded in the same query (no N+1) and paging happens in SQL.
        var items = await ordered
            .ThenBy(c => c.Id)
            .Include(c => c.Policy)
                .ThenInclude(p => p!.InsuranceType)
            .Include(c => c.Handler)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<Claim?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Claims
            .AsNoTracking()
            .Include(c => c.Policy)
                .ThenInclude(p => p!.InsuranceType)
            .Include(c => c.Handler)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Claim?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Claims
            .Include(c => c.Policy)
                .ThenInclude(p => p!.InsuranceType)
            .Include(c => c.Handler)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void Add(Claim claim) => _context.Claims.Add(claim);

    public void Remove(Claim claim) => _context.Claims.Remove(claim);
}
