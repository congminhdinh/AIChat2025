using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var accountService = builder.AddProject<AccountService>("accountservice");
var tenantService = builder.AddProject<TenantService>("tenantservice");
builder.AddProject<ApiGateway>("apigateway")
       // Inject the URLs of the downstream services into the gateway's configuration
       .WithReference(accountService)
       .WithReference(tenantService);
       //// Expose the gateway's endpoint to be accessible from the browser
       //.WithExternalHttpEndpoints();
builder.AddProject<Projects.DocumentService>("documentservice");
       //// Expose the gateway's endpoint to be accessible from the browser
       //.WithExternalHttpEndpoints();
builder.Build().Run();
