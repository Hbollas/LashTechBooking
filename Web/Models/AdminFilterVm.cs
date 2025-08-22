using Domain;

namespace Web.Models;

public class AdminFilterVm
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public AppointmentStatus? Status { get; set; }

    public List<Domain.Appointment> Results { get; set; } = new();
}
