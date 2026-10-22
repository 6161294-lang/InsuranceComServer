namespace InsuranceClaims.Core.Entities;

/// <summary>A line of insurance (car, home, health...).</summary>
public class InsuranceType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Claim> Claims { get; set; } = new List<Claim>();
    public ICollection<Client> Clients { get; set; } = new List<Client>();
    public ICollection<Employee> Specialists { get; set; } = new List<Employee>();
}
