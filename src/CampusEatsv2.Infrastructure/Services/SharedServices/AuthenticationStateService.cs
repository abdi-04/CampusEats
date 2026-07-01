using CampusEatsv2.Core.Models;

namespace CampusEatsv2.Infrastructure.Services.SharedServices;

public class AuthenticationStateService
{
    private object? _currentUser;
    private string? _currentRole;

    private bool _requiresPasswordChange;

    public event Action? StateChanged;

    public object? CurrentUser => _currentUser;
    public string? CurrentRole => _currentRole;

    public bool RequiresPasswordChange => _requiresPasswordChange;

    public bool IsAuthenticated => _currentUser is not null;

    public void SetAuthenticatedCustomer(Customer customer)
    {
        _currentUser = customer;
        _currentRole = "Customer";
        _requiresPasswordChange = false; // customers trenger ikke dette
        NotifyStateChanged();
    }

    public void SetAuthenticatedCourier(Courier courier)
    {
        _currentUser = courier;
        _currentRole = "Courier";
        _requiresPasswordChange = false;
        NotifyStateChanged();
    }

    public void SetAuthenticatedAdmin(Admin admin)
    {
        _currentUser = admin;
        _currentRole = "Admin";

        _requiresPasswordChange = admin.IsFirstLogin;

        NotifyStateChanged();
    }

    public void ClearPasswordChangeRequirement()
    {
        _requiresPasswordChange = false;
        NotifyStateChanged();
    }

    public void Logout()
    {
        _currentUser = null;
        _currentRole = null;
        _requiresPasswordChange = false;
        NotifyStateChanged();
    }

    public Customer? GetCustomer() => _currentUser is Customer customer ? customer : null;
    public Courier? GetCourier() => _currentUser is Courier courier ? courier : null;
    public Admin? GetAdmin() => _currentUser is Admin admin ? admin : null;

    private void NotifyStateChanged() => StateChanged?.Invoke();
}