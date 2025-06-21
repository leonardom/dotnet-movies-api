namespace Movies.Api;

public abstract class AuthConstants
{
    public const string AdminUserPolicyName = "Admin";
    public const string AdminRoleClaimName = "admin";

    public const string EditorUserPolicyName = "Editor";
    public const string EditorRoleClaimName = "editor";
}