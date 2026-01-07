using Microsoft.Extensions.DependencyInjection;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);
builder.Services.Configure<DistributedApplicationOptions>(options => {
   
});
var accountService = builder.AddProject<AccountService>("accountservice");
var tenantService = builder.AddProject<TenantService>("tenantservice");
var storageService = builder.AddProject<StorageService>("storageservice");
var documentService = builder.AddProject<DocumentService>("documentservice")
    .WaitFor(storageService);

var chatService = builder.AddProject<Projects.ChatService>("chatservice")
    .WaitFor(accountService);
var apiGateway = builder.AddProject<ApiGateway>("apigateway")
       .WithReference(accountService)
       .WithReference(tenantService)
       .WithReference(documentService)
       .WithReference(storageService)
       .WithReference(chatService)
       .WaitFor(accountService)
       .WaitFor(tenantService)
       .WaitFor(documentService)
       .WaitFor(storageService)
       .WaitFor(chatService);

builder.AddProject<Projects.WebApp>("webapp")
       .WaitFor(apiGateway);
builder.AddProject<Projects.AdminCMS>("admincms")
       .WaitFor(apiGateway);
builder.Build().Run();