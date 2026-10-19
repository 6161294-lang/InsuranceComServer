namespace InsuranceClaims.Core.Entities;

/// <summary>A line of insurance (car, home, health...). Adjusters specialize in insurance types.</summary>
public class InsuranceType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Policy> Policies { get; set; } = new List<Policy>();
    public ICollection<Employee> Specialists { get; set; } = new List<Employee>();
}
