namespace Dumbo.Domain.Enums;

/// <summary>
/// Represents the lifecycle states of a user account in the system, 
/// matching the numerical values of the shared SQL schema.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// Pending activation state
    /// Assigned to manually created accounts (workers or clients) that have 
    /// not yet defined their password via the activation link. 
    /// The user cannot log in until activation is complete.
    /// </summary>
    PendingActivation = 1,
    
    /// <summary>
    /// Active state (Value: 2). 
    /// Standard operational state for accounts that are fully registered, 
    /// activated, and permitted to interact with the system according to their role.
    /// </summary>
    Active = 2,
    
    /// <summary>
    /// Disabled state
    /// Administrative suspension state. The user is temporarily blocked from logging in, 
    /// but the account data and historical records are preserved.
    /// </summary>
    Disabled = 3,
    
    /// <summary>
    /// Soft-deleted state
    /// Indicates the account has been logically deleted (either by a client requesting 
    /// their own account removal or by administrative action). The record is removed 
    /// from active listings and blocks access, but historical transactions and relationships 
    /// with orders are permanently preserved.
    /// </summary>
    Deleted = 4
}