using Dumbo.Application.Abstractions;
using Dumbo.Application.Email;
using Microsoft.Extensions.Hosting;

namespace Dumbo.Infrastructure.Email;

/// <summary>
/// DEVELOPMENT ONLY: writes each outbound email as a local text file under the API's <c>dev-mail</c> folder
/// (ignored in <c>.gitignore</c>) so end-to-end flows can be tested without a Resend account.
/// While D06 forbids writing tokens to logs, tokens written here go only to a local development file, not logs.
/// </summary>
internal sealed class FileEmailSender(IHostEnvironment environment) : IEmailSender
{
    /// <inheritdoc />
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var folder = Path.Combine(environment.ContentRootPath, "dev-mail");
        Directory.CreateDirectory(folder);

        // Generated file name never includes user or account details.
        var path = Path.Combine(folder, $"{DateTime.UtcNow:yyyyMMdd-HHmmssfff}_{Guid.NewGuid():N}.txt");
        var content = $"Para: {message.To}\nAsunto: {message.Subject}\n\n{message.TextBody}\n";

        await File.WriteAllTextAsync(path, content, cancellationToken);
    }
}