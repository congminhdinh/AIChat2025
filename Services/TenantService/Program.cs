using Infrastructure.Database;
using Infrastructure.Web;
using TenantService.Data;
using TenantService.Endpoints;
using TenantService.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppSettings();
builder.AddInfrastructure();
builder.AddCustomOpenApi();
builder.AddCustomDbContext<TenantDbContext>(builder.Configuration.GetConnectionString(nameof(TenantDbContext)), "TenantService");
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<TenantBusiness>();
builder.Services.AddHttpClient<TenantBusiness>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseInfrastructure();
app.MapOpenApi();
app.MapTenantEndpoints();
//app.MapControllers();

app.Run();
