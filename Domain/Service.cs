namespace Domain;

public class Service
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; } // e.g., 60
    public decimal Price { get; set; }       // e.g., 89.00m
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}
