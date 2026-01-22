using Infrastructure;
using Infrastructure.Web;
using StorageService.Endpoints;
using StorageService.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppSettings();
builder.AddInfrastructure();
builder.AddCustomOpenApi();
builder.Services.Configure<AppSettings>(builder.Configuration);
builder.Services.AddScoped<StorageBusiness>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseInfrastructure();
app.MapOpenApi();
app.MapStorageEndpoints();
//app.MapControllers();

app.Run();
