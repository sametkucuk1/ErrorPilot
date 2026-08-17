using ErrorSimulator.Services;

var builder = WebApplication.CreateBuilder(args);

var applicationInsightsConnectionString =
    builder.Configuration["ApplicationInsights:ConnectionString"];

// appsettings.json'daki boş placeholder ile hiç tanımlanmamış durumu aynı şekilde ele alıyoruz.
if (string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
{
    applicationInsightsConnectionString = null;
}

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Üretilen hatalar Application Insights SDK'sı üzerinden Log Analytics'e gidiyor.
// Bağlantı dizesi user-secrets ya da ortam değişkeninden okunuyor, kod deposunda tutulmuyor.
// SDK bağlantı dizesi olmadan başlatılırsa uygulamayı çökerttiği için kaydı koşula bağlıyoruz;
// böylece ayar tanımlı değilken uygulama yine de çalışıp hataları yerel loglara yazabiliyor.
if (applicationInsightsConnectionString is not null)
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = applicationInsightsConnectionString;
    });
}

builder.Services.AddHostedService<ErrorGeneratorService>();

var app = builder.Build();

// Bağlantı dizesi tanımlı değilse uygulama sorunsuz açılır ama hiçbir telemetri Azure'a
// ulaşmaz; bu sessiz durumu fark etmek zor olduğu için başlangıçta uyarı veriyoruz.
if (applicationInsightsConnectionString is null)
{
    app.Logger.LogWarning(
        "ApplicationInsights:ConnectionString is not configured. "
        + "Errors will only be written to the local log and will not reach Log Analytics.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
