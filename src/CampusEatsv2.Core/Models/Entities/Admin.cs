namespace CampusEatsv2.Core.Models;

/// <summary>
/// Admin model for system administrators
/// Admins have a separate login system and can manage platform operations:
/// - Approve/reject courier registrations
/// - Manage products and menus
/// - View earnings reports and dashboards
/// - Invite other admins or promote existing Customers/Couriers to admin status
/// </summary>
public class Admin
{
    public Guid AdminId { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsFirstLogin {get; set; } = true; // Set isFirstLogin to true when creating a new admin, and require password change on first login

    // Need this in Admin as Customer and Courier don't have a reference to Admin, but we want to track which admin invited/promoted them to admin status.
    public Guid? InvitedByAdminId { get; set; } // Optional reference to the admin who invited this admin link with InvitedByAdmin!
    
}