using System.Net;
using Dumbo.Domain.Enums;

namespace Dumbo.Application.Email;

/// <summary>
/// Builds account-related email messages. Copy and wording are custom application defaults
/// and never include passwords, only a single-use action link.
/// </summary>
public static class AccountEmailTemplates
{
    /// <summary>
    /// Creates an <see cref="EmailMessage"/> for account activation or password reset.
    /// </summary>
    public static EmailMessage Build(ActivationTokenType kind, string firstName, string to, string link)
    {
        var (subject, intro) = kind == ActivationTokenType.AccountActivation
            ? ("Activa tu cuenta de Dumbo Supermercados",
               "Se creó una cuenta para ti en Dumbo Supermercados. Para definir tu contraseña y activarla, abre este enlace:")
            : ("Restablece tu contraseña de Dumbo Supermercados",
               "Recibimos una solicitud para restablecer tu contraseña. Para elegir una nueva, abre este enlace:");

        const string footer =
            "El enlace es válido por 30 minutos y solo se puede usar una vez. Si no lo solicitaste, ignora este correo.";

        // Keep the link on its own plain-text line so test helpers can extract it reliably.
        var text = $"Hola {firstName},\n\n{intro}\n\n{link}\n\n{footer}\n\nDumbo Supermercados";

        var html =
            $"<p>Hola {WebUtility.HtmlEncode(firstName)},</p>" +
            $"<p>{intro}</p>" +
            $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">Abrir enlace</a></p>" +
            $"<p>{footer}</p><p>Dumbo Supermercados</p>";

        return new EmailMessage(to, subject, html, text);
    }
}