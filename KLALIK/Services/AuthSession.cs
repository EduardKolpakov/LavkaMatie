namespace KLALIK.Services;

public class AuthSession
{
    public int? UserId { get; private set; }
    public string? DisplayName { get; private set; }
    public string? RoleName { get; private set; }
    public int? MasterProfileId { get; private set; }

    public bool IsAuthenticated => UserId.HasValue;

    public void SetUser(int userId, string displayName, string roleName, int? masterProfileId)
    {
        UserId = userId;
        DisplayName = displayName;
        RoleName = roleName;
        MasterProfileId = masterProfileId;
    }

    public void Clear()
    {
        UserId = null;
        DisplayName = null;
        RoleName = null;
        MasterProfileId = null;
    }
}
