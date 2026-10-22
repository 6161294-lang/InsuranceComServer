using InsuranceClaims.Core.Common;
using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Enums;
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

    public async Task<(IReadOnlyList<Claim> Items, int TotalCount)> GetPagedAsync(ClaimQueryParameters query, ClaimAccess access, CancellationToken cancellationToken = default)
    {
        IQueryable<Claim> claims = _context.Claims.AsNoTracking();

        claims = ApplyAccess(claims, access, query.Scope);

        if (query.Status is { } status)
        {
            claims = claims.Where(c => c.Status == status);
        }

        if (query.InsuranceTypeId is int typeId)
        {
            claims = claims.Where(c => c.InsuranceTypeId == typeId);
        }

        var totalCount = await claims.CountAsync(cancellationToken);

        IOrderedQueryable<Claim> ordered = (query.SortBy, query.Descending) switch
        {
            (ClaimSortField.Amount, false) => claims.OrderBy(c => c.Amount),
            (ClaimSortField.Amount, true) => claims.OrderByDescending(c => c.Amount),
            (ClaimSortField.Status, false) => claims.OrderBy(c => c.Status),
            (ClaimSortField.Status, true) => claims.OrderByDescending(c => c.Status),
            (_, false) => claims.OrderBy(c => c.OpenedAt),
            _ => claims.OrderByDescending(c => c.OpenedAt)
        };

        // Related data is loaded in the same query (no N+1) and paging happens in SQL.
        var items = await ordered
            .ThenBy(c => c.Id)
            .Include(c => c.Client)
            .Include(c => c.InsuranceType)
            .Include(c => c.Handler)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private static IQueryable<Claim> ApplyAccess(IQueryable<Claim> claims, ClaimAccess access, ClaimScope scope)
    {
        if (access.ClientId is int clientId)
        {
            return claims.Where(c => c.ClientId == clientId);
        }

        // Drafts are private to the client who is writing them.
        claims = claims.Where(c => c.Status != ClaimStatus.Draft);

        if (access.AppraiserId is not int appraiserId)
        {
            return claims; // manager: every non-draft claim
        }

        var specializations = access.SpecializationIds;
        return scope switch
        {
            ClaimScope.Mine => claims.Where(c => c.HandlerId == appraiserId),
            ClaimScope.Available => claims.Where(c => c.Status == ClaimStatus.Opened && c.HandlerId == null && specializations.Contains(c.InsuranceTypeId)),
            _ => claims.Where(c => c.HandlerId == appraiserId ||
                                   (c.Status == ClaimStatus.Opened && c.HandlerId == null && specializations.Contains(c.InsuranceTypeId)))
        };
    }

    public Task<Claim?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Claims
            .AsNoTracking()
            .Include(c => c.Client)
            .Include(c => c.InsuranceType)
            .Include(c => c.Handler)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Claim?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Claims
            .Include(c => c.Client)
            .Include(c => c.InsuranceType)
            .Include(c => c.Handler)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void Add(Claim claim) => _context.Claims.Add(claim);

    public void Remove(Claim claim) => _context.Claims.Remove(claim);
}
