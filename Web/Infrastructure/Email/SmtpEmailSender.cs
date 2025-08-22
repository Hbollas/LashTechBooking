using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Web.Infrastructure.Email;

public class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string User { get; set; } = "";
    public string Pass { get; set; } = "";
    public string FromEmail { get; set; } = "no-reply@example.com";
    public string FromName { get; set; } = "AA's Aesthetics";
}

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _opt;
    public SmtpEmailSender(IOptions<SmtpOptions> opt) => _opt = opt.Value;

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // If not configured, just log to console so dev isn’t blocked
        if (string.IsNullOrWhiteSpace(_opt.Host))
        {
            Console.WriteLine($"[EMAIL -> {email}] {subject}\n{htmlMessage}\n");
            return;
        }

        using var client = new SmtpClient(_opt.Host, _opt.Port)
        {
            EnableSsl = _opt.UseSsl,
            Credentials = new NetworkCredential(_opt.User, _opt.Pass)
        };
        using var msg = new MailMessage
        {
            From = new MailAddress(_opt.FromEmail, _opt.FromName),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };
        msg.To.Add(email);
        await client.SendMailAsync(msg);
    }
}
