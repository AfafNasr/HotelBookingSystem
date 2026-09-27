using HotelBooking.Application.Emails;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HotelBooking.Infrastructure.Emails;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpEmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<SmtpEmailOptions> options,
         ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            var email = new MimeMessage();

            email.From.Add(
                new MailboxAddress(
                    _options.FromName,
                    _options.FromEmail));

            email.To.Add(
                MailboxAddress.Parse(message.To));

            email.Subject = message.Subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = message.HtmlBody
            };

            email.Body =
                bodyBuilder.ToMessageBody();

            using var smtpClient =
                new SmtpClient();

            await smtpClient.ConnectAsync(
                _options.Host,
                _options.Port,
                SecureSocketOptions.StartTls,
                cancellationToken);

            await smtpClient.AuthenticateAsync(
                _options.Username,
                _options.Password,
                cancellationToken);

            await smtpClient.SendAsync(
                email,
                cancellationToken);

            await smtpClient.DisconnectAsync(
                true,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to send email with subject {Subject}.",
                message.Subject);

            throw;
        }
    }
}