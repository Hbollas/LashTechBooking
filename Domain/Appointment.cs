namespace Domain;

public enum AppointmentStatus { Pending, Confirmed, Cancelled }

public class Appointment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Link to the service being booked
    public Guid ServiceId { get; set; }
    public Service? Service { get; set; }

    // Customer basics (we’ll add Identity later)
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }

    // Time window in UTC (safer for storage)
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    // Stripe later — placeholders now
    public string? PaymentIntentId { get; set; }
    public bool DepositPaid { get; set; }
}
