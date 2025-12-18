using DocumentService.Data;
using DocumentService.Endpoints;
using DocumentService.Features;
using Hangfire;
using Infrastructure;
using Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppSettings();
builder.AddInfrastructure();
var appSettings = builder.Configuration.Get<AppSettings>()?? new AppSettings();
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<PromptDocumentBusiness>();
builder.Services.AddScoped<VectorizeBackgroundJob>();
builder.Services.AddHttpClient<PromptDocumentBusiness>();
builder.Services.AddHttpClient<VectorizeBackgroundJob>();
builder.AddCustomDbContext<DocumentDbContext>(builder.Configuration.GetConnectionString(nameof(DocumentDbContext)), "DocumentService");
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString(nameof(DocumentDbContext))));
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 5;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseInfrastructure();
app.MapDocumentEndpoints();
app.UseHangfireDashboard("/hangfire");
//app.MapControllers();

app.Run();
