using Dumbo.Application.Abstractions;
using Dumbo.Application.Accounts;
using Dumbo.Domain.Enums;

namespace Dumbo.Application.Clients;

/// <summary>
/// Raw query parameters accepted by the client listing endpoint; all fields are optional
/// </summary>
public sealed record ListClientsCommand(string? Search, string? Status, int? Page, int? PageSize);

/// <summary>
/// Lists active/non-deleted client accounts for staff administration,
/// normalizing pagination parameters and validating status filters before querying.
/// </summary>
public sealed class ListClientsUseCase(IClientAdminStore store)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    private const int MaxPage = 100_000;  
    private const int MaxSearchLength = 100;

    // Deleted accounts are excluded and handled in a separate view
    private static readonly UserStatus[] ListableStatuses =
        [UserStatus.PendingActivation, UserStatus.Active, UserStatus.Disabled];

    /// <summary>
    /// Validates and normalizes filter parameters and returns a paged list of client summaries
    /// </summary>
    public async Task<ListClientsResult> ExecuteAsync(ListClientsCommand command, CancellationToken cancellationToken)
    {
        UserStatus? status = null;
        if (!string.IsNullOrWhiteSpace(command.Status))
        {
            var text = command.Status.Trim();
            foreach (var candidate in ListableStatuses)
            {
                // Match by enum name only so numeric strings like "2" or "99" are rejected
                if (candidate.ToString().Equals(text, StringComparison.OrdinalIgnoreCase))
                    status = candidate;
            }

            if (status is null)
                return new ListClientsResult(Errors: new Dictionary<string, string[]>
                {
                    ["status"] = ["Estado no válido. Usa PendingActivation, Active o Disabled"]
                });
        }

        // Clamp out-of-range pagination values instead of failing the request
        var page = Math.Clamp(command.Page ?? 1, 1, MaxPage);
        var pageSize = Math.Clamp(command.PageSize ?? DefaultPageSize, 1, MaxPageSize);

        var search = command.Search?.Trim();
        if (string.IsNullOrEmpty(search))
            search = null;
        else if (search.Length > MaxSearchLength)
            search = search[..MaxSearchLength];

        var result = await store.ListAsync(new ClientQuery(search, status, page, pageSize), cancellationToken);

        return new ListClientsResult(new ClientPage(
            result.Items.Select(u => u.ToSummary()).ToList(), result.Total, page, pageSize));
    }
}