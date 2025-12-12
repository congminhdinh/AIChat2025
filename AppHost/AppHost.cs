using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var accountService = builder.AddProject<AccountService>("accountservice");
var tenantService = builder.AddProject<TenantService>("tenantservice");
var documentService =  builder.AddProject<DocumentService>("documentservice");
var storageService =  builder.AddProject<StorageService>("storageservice");
builder.AddProject<ApiGateway>("apigateway")
       // Inject the URLs of the downstream services into the gateway's configuration
       .WithReference(accountService)
       .WithReference(tenantService)
       .WithReference(documentService)
       .WithReference(storageService);
//.WithExternalHttpEndpoints();
builder.Build().Run();
