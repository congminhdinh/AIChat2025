using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var accountService = builder.AddProject<AccountService>("accountservice");
var tenantService = builder.AddProject<TenantService>("tenantservice");
var documentService = builder.AddProject<DocumentService>("documentservice");
builder.AddProject<ApiGateway>("apigateway")
       // Inject the URLs of the downstream services into the gateway's configuration
       .WithReference(accountService)
       .WithReference(tenantService)
       .WithReference(documentService);

builder.Build().Run();
