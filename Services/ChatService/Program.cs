using ChatService.Consumers;
using ChatService.Data;
using ChatService.Endpoints;
using ChatService.Events;
using ChatService.Features;
using ChatService.Hubs;
using Infrastructure;
using Infrastructure.Database;
using MassTransit;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureAppSettings();

builder.AddInfrastructure();
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureEndpointDefaults(defaults =>
    {
        defaults.Protocols = HttpProtocols.Http1AndHttp2;
    });
});
// Add Repository pattern
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));

// Add DbContext
builder.AddCustomDbContext<ChatDbContext>(
    builder.Configuration.GetConnectionString(nameof(ChatDbContext)),
    "ChatService");

// Add Business Logic
builder.Services.AddScoped<ChatBusiness>();

// Add SignalR
builder.AddCustomSignalR();

// Add MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    // Register consumers
    x.AddConsumer<BotResponseConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var appSettings = context.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<AppSettings>>().CurrentValue;
        var rabbitMqEndpoint = appSettings.RabbitMQEndpoint ?? "localhost:5672";
        var username = appSettings.RabbitMQUsername ?? "guest";
        var password = appSettings.RabbitMQPassword ?? "guest";

        cfg.Host($"rabbitmq://{rabbitMqEndpoint}", h =>
        {
            h.Username(username);
            h.Password(password);
        });

        // Configure queue for consuming bot responses
        cfg.ReceiveEndpoint("BotResponseCreated", e =>
        {
            e.ConfigureConsumer<BotResponseConsumer>(context);
        });

        // Configure publishing endpoint for user prompts
        cfg.Message<UserPromptReceivedEvent>(e =>
        {
            e.SetEntityName("UserPromptReceived");
        });

        cfg.Publish<UserPromptReceivedEvent>(e =>
        {
            e.ExchangeType = "fanout";
        });
    });
});

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS (for SignalR clients)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // Add your frontend URLs
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.UseInfrastructure();

// Map SignalR Hub
app.MapHub<ChatHub>("/hubs/chat");

// Map REST API Endpoints
app.MapChatEndpoints();

app.Run();
