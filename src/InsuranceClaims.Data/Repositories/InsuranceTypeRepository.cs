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
        await _context.InsuranceTypes.AsNoTracking().OrderBy(t => t.Name).ToListAsync(cancellationToken);

    public Task<InsuranceType?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.InsuranceTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<InsuranceType?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        _context.InsuranceTypes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<InsuranceType>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var distinctIds = ids.Distinct().ToList();
        return await _context.InsuranceTypes.Where(t => distinctIds.Contains(t.Id)).ToListAsync(cancellationToken);
    }

    public Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default) =>
        _context.InsuranceTypes.AnyAsync(t => t.Name == name && (excludeId == null || t.Id != excludeId), cancellationToken);

    public async Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Claims.AnyAsync(c => c.InsuranceTypeId == id, cancellationToken)
        || await _context.Clients.AnyAsync(c => c.InsuranceTypes.Any(t => t.Id == id), cancellationToken)
        || await _context.Employees.AnyAsync(e => e.Specializations.Any(t => t.Id == id), cancellationToken);

    public void Add(InsuranceType insuranceType) => _context.InsuranceTypes.Add(insuranceType);

    public void Remove(InsuranceType insuranceType) => _context.InsuranceTypes.Remove(insuranceType);
}
