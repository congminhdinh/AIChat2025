using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var accountService = builder.AddProject<AccountService>("accountservice");
var tenantService = builder.AddProject<TenantService>("tenantservice");
var documentService =  builder.AddProject<DocumentService>("documentservice");
var storageService =  builder.AddProject<StorageService>("storageservice");
var chatService = builder.AddProject<Projects.ChatService>("chatservice");
builder.AddProject<ApiGateway>("apigateway")
       // Inject the URLs of the downstream services into the gateway's configuration
       .WithReference(accountService)
       .WithReference(tenantService)
       .WithReference(documentService)
       .WithReference(storageService)
       .WithReference(chatService);

//.WithExternalHttpEndpoints();
builder.AddProject<Projects.WebApp>("webapp");

//.WithExternalHttpEndpoints();
builder.Build().Run();
