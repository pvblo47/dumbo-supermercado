using System.Text;

namespace Dumbo.Infrastructure.Persistence;

/// <summary>
/// Utility helper for converting PascalCase or camelCase identifiers into snake_case
/// for PostgreSQL table, column, and constraint naming conventions.
/// </summary>
internal static class SnakeCase
{
    /// <summary>
    /// Converts a string identifier into snake_case (for example, <c>"PasswordHash"</c> to <c>"password_hash"</c>).
    /// </summary>
    public static string Convert(string name)
    {
        var sb = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1])))
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}