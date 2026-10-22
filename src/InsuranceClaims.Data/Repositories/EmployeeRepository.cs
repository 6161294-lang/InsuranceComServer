using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InsuranceClaims.Data.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;

    public EmployeeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(PagingParameters paging, CancellationToken cancellationToken = default)
    {
        var totalCount = await _context.Employees.CountAsync(cancellationToken);

        var items = await _context.Employees
            .AsNoTracking()
            .OrderBy(e => e.FullName)
            .ThenBy(e => e.Id)
            .Include(e => e.Specializations)
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Employees
            .AsNoTracking()
            .Include(e => e.Specializations)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<Employee?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Employees
            .Include(e => e.Specializations)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _context.Employees
            .AsNoTracking()
            .Include(e => e.Specializations)
            .FirstOrDefaultAsync(e => e.Email == email, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        _context.Employees.AnyAsync(e => e.Email == email, cancellationToken);

    public async Task<bool> HasWorkHistoryAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Claims.AnyAsync(c => c.HandlerId == id, cancellationToken)
        || await _context.Referrals.AnyAsync(r => r.AppraiserId == id, cancellationToken);

    public void Add(Employee employee) => _context.Employees.Add(employee);

    public void Remove(Employee employee) => _context.Employees.Remove(employee);
}
