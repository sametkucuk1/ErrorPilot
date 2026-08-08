using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
using Azure.Monitor.Query;
using ErrorPilotEngine.Models;
using ErrorPilotEngine.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddOptions<LogAnalyticsOptions>()
    .Bind(builder.Configuration.GetSection(LogAnalyticsOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<GeminiOptions>()
    .Bind(builder.Configuration.GetSection(GeminiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<SlackOptions>()
    .Bind(builder.Configuration.GetSection(SlackOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<ErrorMonitorOptions>()
    .Bind(builder.Configuration.GetSection(ErrorMonitorOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<TokenCredential>(_ => new DefaultAzureCredential());
builder.Services.AddSingleton(serviceProvider =>
    new LogsQueryClient(serviceProvider.GetRequiredService<TokenCredential>()));

builder.Services.AddSingleton<IErrorWatermarkStore, ErrorWatermarkStore>();

builder.Services.AddScoped<ILogAnalyticsService, LogAnalyticsService>();
builder.Services.AddScoped<IErrorAnalysisService, ErrorAnalysisService>();

builder.Services.AddHttpClient<IAiAnalysisService, GeminiAnalysisService>((serviceProvider, client) =>
{
    var geminiOptions = serviceProvider.GetRequiredService<IOptions<GeminiOptions>>().Value;

    client.BaseAddress = new Uri(geminiOptions.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(geminiOptions.RequestTimeoutSeconds);
});

builder.Services.AddHttpClient<ISlackNotificationService, SlackNotificationService>((serviceProvider, client) =>
{
    var slackOptions = serviceProvider.GetRequiredService<IOptions<SlackOptions>>().Value;

    client.Timeout = TimeSpan.FromSeconds(slackOptions.RequestTimeoutSeconds);
});

if (builder.Configuration
    .GetSection(ErrorMonitorOptions.SectionName)
    .Get<ErrorMonitorOptions>()?.Enabled ?? true)
{
    builder.Services.AddHostedService<ErrorMonitorService>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
