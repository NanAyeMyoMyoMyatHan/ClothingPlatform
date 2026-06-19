using ClothingPlatform.DB.AppDbModels;

public class SessionState
{
    public TblUser? CurrentUser { get; private set; }
    public bool IsLoggedIn => CurrentUser != null;

    // ✅ Dynamic RBAC helpers — uses TblRole navigation property
    public string? RoleName => CurrentUser?.Role?.RoleName;

    public bool IsInRole(string role) =>
        string.Equals(RoleName, role, StringComparison.OrdinalIgnoreCase);

    public bool HasDashboardAccess =>
        IsInRole("admin") || IsInRole("staff");

    public bool IsAdmin => IsInRole("admin");
    public bool IsStaff => IsInRole("staff");
    public bool IsCustomer => IsInRole("customer");

    // ✅ Dynamic permission check from TblRolePermission
    public List<string> Permissions { get; private set; } = new();

    public bool HasPermission(string permission) =>
        Permissions.Any(p => string.Equals(p, permission, StringComparison.OrdinalIgnoreCase));

    public void Login(TblUser user, List<string>? permissions = null)
    {
        CurrentUser = user;
        Permissions = permissions ?? new List<string>();
        NotifyStateChanged();
    }

    public void Logout()
    {
        CurrentUser = null;
        Permissions = new List<string>();
        NotifyStateChanged();
    }

    public event Action? OnChange;
    public void NotifyStateChanged() => OnChange?.Invoke();
}