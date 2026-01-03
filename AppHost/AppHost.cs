using Microsoft.Extensions.DependencyInjection;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);
builder.Services.Configure<DistributedApplicationOptions>(options => {
   
});
// 1. Kh?i t?o các Service c? s? (Back-end Services)
var accountService = builder.AddProject<AccountService>("accountservice");
var tenantService = builder.AddProject<TenantService>("tenantservice");
var storageService = builder.AddProject<StorageService>("storageservice");

// 2. Các Service nghi?p v? cao h?n nên ??i Service c? s?
var documentService = builder.AddProject<DocumentService>("documentservice")
    .WaitFor(storageService); // Ví d?: C?n storage ?? l?u tài li?u

var chatService = builder.AddProject<Projects.ChatService>("chatservice")
    .WaitFor(accountService); // C?n account ?? xác th?c

// 3. ApiGateway CH? kh?i ??ng khi các service phía sau ?ã s?n sàng
var apiGateway = builder.AddProject<ApiGateway>("apigateway")
       .WithReference(accountService)
       .WithReference(tenantService)
       .WithReference(documentService)
       .WithReference(storageService)
       .WithReference(chatService)
       // Thêm c? ch? ??i ?? tránh xung ??t Port và File Lock khi build
       .WaitFor(accountService)
       .WaitFor(tenantService)
       .WaitFor(documentService)
       .WaitFor(storageService)
       .WaitFor(chatService);

// 4. WebApp ch? ch?y khi Gateway ?ã lên (?? có th? g?i API)
builder.AddProject<Projects.WebApp>("webapp")
       .WaitFor(apiGateway);

builder.Build().Run();