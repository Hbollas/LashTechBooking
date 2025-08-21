namespace Web.Models;

public class CreateBookingVm
{
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int ServiceDurationMinutes { get; set; }

    // Default to today
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    // List of "HH:mm" strings (e.g., 10:00, 10:30, ...)
    public List<string> TimeOptions { get; set; } = new();
}
