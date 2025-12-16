using ChatService.Data;
using Infrastructure;
using Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppSettings();
builder.AddInfrastructure();

builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));
builder.AddCustomDbContext<ChatDbContext>(builder.Configuration.GetConnectionString(nameof(ChatDbContext)), "ChatService");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseInfrastructure();
//app.MapControllers();

app.Run();
