using AccountService.Data;
using AccountService.Endpoints;
using AccountService.Features;
using Infrastructure;
using Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Host.ConfigureAppSettings();
builder.AddInfrastructure();
builder.Services.AddScoped<AccountBusiness>();
builder.AddCustomDbContext<AccountDbContext>(builder.Configuration.GetConnectionString(nameof(AccountDbContext)), "AccountService");
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseInfrastructure();
app.MapAccountEndpoints();
//app.MapControllers();

app.Run();
