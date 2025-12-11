using System.Net;
using System.Net.Mail;

namespace Api.Services;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendVerificationCodeAsync(string toEmail, string code)
    {
        var host = _config["Email:SmtpHost"];
        var port = int.TryParse(_config["Email:SmtpPort"], out var p) ? p : 587;
        var user = _config["Email:User"];
        var pass = _config["Email:Password"];
        var from = _config["Email:From"] ?? user;

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            // Si no hay SMTP configurado, no lanzamos excepción: solo simulamos en consola.
            Console.WriteLine($"[SIMULACIÓN EMAIL] Enviar a {toEmail}: código de verificación = {code}");
            return;
        }

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, pass),
            EnableSsl = true
        };

        var mail = new MailMessage(from, toEmail)
        {
            Subject = "Código de verificación AntMaster",
            Body = $"Tu código de verificación es: {code}",
        };

        await client.SendMailAsync(mail);
    }
}


