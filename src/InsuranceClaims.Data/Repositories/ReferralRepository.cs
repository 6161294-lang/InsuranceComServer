using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaims.Data.Repositories;

public class ReferralRepository : IReferralRepository
{
    private readonly AppDbContext _context;

    public ReferralRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<Referral> Items, int TotalCount)> GetPagedByAppraiserAsync(int appraiserId, PagingParameters paging, CancellationToken cancellationToken = default)
    {
        var referrals = _context.Referrals.AsNoTracking().Where(r => r.AppraiserId == appraiserId);

        var totalCount = await referrals.CountAsync(cancellationToken);

        var items = await referrals
            .OrderByDescending(r => r.ReferredAt)
            .ThenBy(r => r.Id)
            .Include(r => r.Appraiser)
            .Include(r => r.Claim).ThenInclude(c => c!.Client)
            .Include(r => r.Claim).ThenInclude(c => c!.InsuranceType)
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<Referral?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Referrals
            .AsNoTracking()
            .Include(r => r.Appraiser)
            .Include(r => r.Claim).ThenInclude(c => c!.Client)
            .Include(r => r.Claim).ThenInclude(c => c!.InsuranceType)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<Referral?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Referrals
            .Include(r => r.Appraiser)
            .Include(r => r.Claim).ThenInclude(c => c!.Client)
            .Include(r => r.Claim).ThenInclude(c => c!.InsuranceType)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public void Add(Referral referral) => _context.Referrals.Add(referral);
}
