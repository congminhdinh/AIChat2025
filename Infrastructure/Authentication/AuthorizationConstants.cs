namespace Infrastructure.Authentication
{
    public class AuthorizationConstants
    {
        public const string SCOPE_ADMIN = "admin";
        public const string SCOPE_WEB = "web";
        public static readonly string[] SCOPE_ALL = { SCOPE_ADMIN, SCOPE_WEB };
        public const string TOKEN_CLAIMS_TYPE_SCOPE = "Scope";
        public const string TOKEN_CLAIMS_TENANT = "Tenant";
        public const string JWT_SECRET_KEY = "45dfghdfgh2345kfhdfgh2fg34534523sdfgse45";
        public const string POLICY_ADMIN = "AdminPolicy";
    }
}
