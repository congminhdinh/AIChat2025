using AccountService.Data;
using AccountService.Endpoints;
using AccountService.Features;
using Infrastructure;
using Infrastructure.Database;
using Infrastructure.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppSettings();
builder.AddInfrastructure();
builder.AddCustomOpenApi();
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<AccountBusiness>();
builder.Services.AddHttpClient<AuthBusiness>();
builder.AddCustomDbContext<AccountDbContext>(builder.Configuration.GetConnectionString(nameof(AccountDbContext)), "AccountService");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseInfrastructure();
app.MapOpenApi();
app.MapAuthEndpoints();
app.MapAccountEndpoints();
//app.MapControllers();

app.Run();
