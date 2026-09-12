namespace Hub.Web.Authentication;

static class AuthorizationPolicies
{
    public const string Admin = "admin";
}

static class IdentityClaims
{
    public const string Role = "id.user.role";
    public const string AdminRole = "id.user.role.admin";
}
