using Infrastructure.Database;
using TenantService.Data;
using TenantService.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppSettings();
builder.AddInfrastructure();
builder.AddCustomDbContext<TenantDbContext>(builder.Configuration.GetConnectionString(nameof(TenantDbContext)), "TenantService");
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseInfrastructure();
app.MapTenantEndpoints();
//app.MapControllers();

app.Run();
