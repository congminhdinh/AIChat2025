var builder = DistributedApplication.CreateBuilder(args);
builder.AddProject<Projects.AccountService>("accountservice");
builder.AddProject<Projects.TenantService>("tenantservice");
builder.Build().Run();
