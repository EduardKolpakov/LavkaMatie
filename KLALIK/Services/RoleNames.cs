namespace KLALIK.Services;

public static class RoleNames
{
    public const string Client = "Client";
    public const string Master = "Master";
    public const string Moderator = "Moderator";
    public const string Administrator = "Administrator";

    public static bool IsAtLeast(string actualRole, string requiredRole)
    {
        var order = new[] { Client, Master, Moderator, Administrator };
        var ai = Array.IndexOf(order, actualRole);
        var ri = Array.IndexOf(order, requiredRole);
        if (ai < 0 || ri < 0)
            return false;
        return ai >= ri;
    }
}
