namespace Domain;

public class Service
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // Length of the appointment in minutes (e.g., 60, 90)
    public int DurationMinutes { get; set; }

    // Store money as integer cents to avoid SQLite decimal issues
    public int PriceCents { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Description { get; set; }

    // navigation
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
