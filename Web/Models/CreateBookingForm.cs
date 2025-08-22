using System.ComponentModel.DataAnnotations;

namespace Web.Models;

public class CreateBookingForm
{
    [Required] public Guid ServiceId { get; set; }

    [Required] public DateOnly Date { get; set; }

    // "HH:mm" e.g., 14:30
    [Required, RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Pick a time slot.")]
    public string Time { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string CustomerEmail { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Phone (optional)")]
    [RegularExpression(@"^\(?\d{3}\)?[-. ]?\d{3}[-. ]?\d{4}$",
        ErrorMessage = "Please enter a valid US phone number, e.g. (414) 555-0123")]
    public string? CustomerPhone { get; set; }

}
