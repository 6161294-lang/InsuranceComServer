using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaims.Data.Repositories;

public class InsuranceTypeRepository : IInsuranceTypeRepository
{
    private readonly AppDbContext _context;

    public InsuranceTypeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<InsuranceType>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.InsuranceTypes
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

    public Task<InsuranceType?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.InsuranceTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<InsuranceType?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        _context.InsuranceTypes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<InsuranceType>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        return await _context.InsuranceTypes
            .Where(t => idList.Contains(t.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default) =>
        _context.InsuranceTypes.AnyAsync(t => t.Name == name && (excludeId == null || t.Id != excludeId), cancellationToken);

    public Task<bool> HasPoliciesAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Policies.AnyAsync(p => p.InsuranceTypeId == id, cancellationToken);

    public void Add(InsuranceType insuranceType) => _context.InsuranceTypes.Add(insuranceType);

    public void Remove(InsuranceType insuranceType) => _context.InsuranceTypes.Remove(insuranceType);
}
