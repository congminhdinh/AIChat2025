using AccountService.Data;
using AccountService.Endpoints;
using AccountService.Endpoints;
using AccountService.Features;
using Infrastructure;
using Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppSettings();
builder.AddInfrastructure();

builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<AccountBusiness>();
builder.Services.AddScoped<AuthBusiness>();
builder.AddCustomDbContext<AccountDbContext>(builder.Configuration.GetConnectionString(nameof(AccountDbContext)), "AccountService");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseInfrastructure();
app.MapAuthEndpoints();
app.MapAccountEndpoints();
//app.MapControllers();

app.Run();
