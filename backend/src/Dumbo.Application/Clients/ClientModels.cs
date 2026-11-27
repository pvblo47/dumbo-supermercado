using Dumbo.Application.Accounts;

namespace Dumbo.Application.Clients;

/// <summary>Represents a page of client summaries along with the total matching count and applied pagination parameters
/// </summary>
public sealed record ClientPage(IReadOnlyList<AccountSummary> Items, int Total, int Page, int PageSize);

/// <summary>
/// Result of listing clients; when <see cref="Errors"/> is populated, filter validation failed and <see cref="Page"/> is <see langword="null"/>
/// </summary>
public sealed record ListClientsResult(
    ClientPage? Page = null, IReadOnlyDictionary<string, string[]>? Errors = null);

/// <summary>
/// Outcome states for updating a client's profile details
/// </summary>
public enum UpdateClientOutcome { Updated, Invalid, NotFound, EmailTaken }

/// <summary>
/// Result of attempting to update a client account
/// </summary>
public sealed record UpdateClientResult(
    UpdateClientOutcome Outcome,
    AccountSummary? Account = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);

/// <summary>
/// Outcome states for resending an account activation link
/// </summary>
public enum ResendActivationOutcome { Queued, NotFound, NotPending }

/// <summary>
/// Requested administrative status transition for a client account
/// </summary>
public enum ClientStatusAction { Disable, Enable }

/// <summary>
/// Outcome of changing a client's status; <see cref="Changed"/> also covers already being in the target state
/// </summary>
public enum ChangeClientStatusOutcome { Changed, NotFound }

/// <summary>
/// Result of changing a client's administrative status
/// </summary>
public sealed record ChangeClientStatusResult(ChangeClientStatusOutcome Outcome, AccountSummary? Account = null);