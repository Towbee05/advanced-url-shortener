using MailKit.Net.Smtp;
using Fluid;
using Microsoft.Extensions.Options;
using MimeKit;
using UrlShortener.Entities;
using UrlShortener.DTO.Response;

namespace UrlShortener.Services;


public interface IEmailService
{
    Task<bool> SendAsync(string toEmail, string subject, string htmlTemplateName, object templateModel);
    Task<bool> SendVerificationMailAsync(string toEmail, VerificationEmailModel model);
    Task<bool> SendPasswordResetMailAsync(string toEmail, ForgotPasswordEmailModel model);
}

public class EmailService : IEmailService
{
    private readonly FluidParser _parser = new();
    private readonly SMTPSettings _smtpSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SMTPSettings> settings, ILogger<EmailService> logger)
    {
        this._smtpSettings = settings.Value;
        this._logger = logger;
    }

    private async Task<string> RenderTemplateAsync(string templateName, object model)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Templates", templateName);
        var source = await File.ReadAllTextAsync(path);

        if (!_parser.TryParse(source, out var template, out var error))
        {
            throw new Exception($"Failed to parse template: {error}");
        }

        return await template.RenderAsync(new TemplateContext(model));
    }

    public async Task<bool> SendAsync(string toEmail, string subject, string htmlTemplateName, object templateModel)
    {
        try
        {
            var message = new MimeMessage();
            MailboxAddress fromMailboxAddress = new MailboxAddress(name: null, address: this._smtpSettings.EMAIL_SENDER);
            message.From.Add(fromMailboxAddress);

            MailboxAddress toMailAddress = new MailboxAddress(name: null, address: toEmail);
            message.To.Add(toMailAddress);

            message.Subject = subject;
            message.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = await RenderTemplateAsync(htmlTemplateName, templateModel)
            };

            using (SmtpClient client = new SmtpClient())
            {
                await client.ConnectAsync(this._smtpSettings.EMAIL_HOST, this._smtpSettings.EMAIL_PORT, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(this._smtpSettings.EMAIL_USERNAME, this._smtpSettings.EMAIL_PASSWORD);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            return true;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to send mail");
            return false;
        }
    }

    public async Task<bool> SendPasswordResetMailAsync(string toEmail, ForgotPasswordEmailModel model)
    {
        return await SendAsync(toEmail, "Reset your password", "reset-password.liquid", model);
    }

    public async Task<bool> SendVerificationMailAsync(string toEmail, VerificationEmailModel model)
    {
        return await SendAsync(toEmail, "Verify your account", "verification-email.liquid", model);
    }
}