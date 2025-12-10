using DocumentService.Data;
using DocumentService.Endpoints;
using DocumentService.Features;
using Infrastructure;
using Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppSettings();
builder.AddInfrastructure();
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<PromptDocumentBusiness>();
builder.AddCustomDbContext<DocumentDbContext>(builder.Configuration.GetConnectionString(nameof(DocumentDbContext)), "DocumentService");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseInfrastructure();
app.MapDocumentEndpoints();
//app.MapControllers();

app.Run();
