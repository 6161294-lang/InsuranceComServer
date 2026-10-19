using InsuranceClaims.Core.DTOs;
using InsuranceClaims.Core.Entities;
using InsuranceClaims.Core.Enums;
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
        IQueryable<Employee> employees = _context.Employees.AsNoTracking();

        var totalCount = await employees.CountAsync(cancellationToken);

        var items = await employees
            .OrderBy(e => e.FullName)
            .ThenBy(e => e.Id)
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .Include(e => e.Specializations)
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

    public Task<Employee?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        _context.Employees
            .AsNoTracking()
            .Include(e => e.Specializations)
            .FirstOrDefaultAsync(e => e.Username == username, cancellationToken);

    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default) =>
        _context.Employees.AnyAsync(e => e.Username == username, cancellationToken);

    public Task<bool> HasClaimsInProgressAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Claims.AnyAsync(c => c.HandlerId == id && c.Status == ClaimStatus.InProgress, cancellationToken);

    public void Add(Employee employee) => _context.Employees.Add(employee);

    public void Remove(Employee employee) => _context.Employees.Remove(employee);
}
