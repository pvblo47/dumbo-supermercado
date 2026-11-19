using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dumbo.Application.Abstractions;
using Dumbo.Application.Email;
using Microsoft.Extensions.Configuration;

namespace Dumbo.Infrastructure.Email;

/// <summary>
/// Sends emails through the Resend HTTPS API using <c>Resend:ApiKey</c> and <c>Resend:From</c>
/// (a sender address from a verified Resend domain, never stored in Git).
/// Delivery failures are translated into <see cref="EmailDeliveryException"/> with safe messages
/// that omit email body content and API credentials.
/// </summary>
internal sealed class ResendEmailSender(HttpClient http, IConfiguration configuration) : IEmailSender
{
    /// <inheritdoc />
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var apiKey = configuration["Resend:ApiKey"];
        var from = configuration["Resend:From"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("Faltan 'Resend:ApiKey' o 'Resend:From'.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(new
            {
                from,
                to = new[] { message.To },
                subject = message.Subject,
                html = message.HtmlBody,
                text = message.TextBody
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new EmailDeliveryException($"El proveedor de correo respondió {(int)response.StatusCode}.");
        }
        catch (HttpRequestException)
        {
            throw new EmailDeliveryException("No se pudo contactar al proveedor de correo.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new EmailDeliveryException("El proveedor de correo no respondió a tiempo.");
        }
    }
}