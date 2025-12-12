namespace DocumentService.Endpoints
{
    public static class DocumentEndpoint
    {
        public static void MapDocumentEndpoints(this WebApplication app)
        {
            app.MapWebApiGroups();
        }
        static void MapWebApiGroups(this IEndpointRouteBuilder app)
        {
            //var group = app.MapWebApiGroup("accounts");
            //group.MapGet("/ok", () => Results.Ok("Account service is running"));
            //group.MapPost("/register", async (PromptDocumentBusiness DocumentBusiness, [FromBody] RegisterRequest input, int tenantId) =>
            //{
            //    return await DocumentBusiness.Register(input, tenantId);
            //}).AllowAnonymous();
            //group.MapPost("/login", async (PromptDocumentBusiness DocumentBusiness, LoginRequest input, int tenantId) =>
            //{
            //    return await DocumentBusiness.Login(input, tenantId);
            //}).AllowAnonymous();
        }
    }
}
