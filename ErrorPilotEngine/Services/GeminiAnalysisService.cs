using System.Net;
using System.Net.Http.Json;
using System.Text;
using ErrorPilotEngine.Models;
using Microsoft.Extensions.Options;

namespace ErrorPilotEngine.Services;

public class GeminiAnalysisService : IAiAnalysisService
{
    private const string ApiKeyHeaderName = "x-goog-api-key";

    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiAnalysisService> _logger;

    public GeminiAnalysisService(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiAnalysisService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> AnalyzeErrorAsync(ErrorRecord error, CancellationToken cancellationToken)
    {
        using var request = BuildRequestMessage(error);

        using var response = await SendRequestAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new AiRateLimitExceededException(
                "Gemini rate limit reached (HTTP 429). Analysis stopped without retrying.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogError(
                "Gemini returned HTTP {StatusCode} for model {Model}.",
                (int)response.StatusCode,
                _options.Model);

            throw new AiAnalysisException(
                $"Gemini returned HTTP {(int)response.StatusCode}: {errorBody}");
        }

        var payload = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken);

        return ExtractAnalysisText(payload);
    }

    private HttpRequestMessage BuildRequestMessage(ErrorRecord error)
    {
        var payload = new GeminiRequest
        {
            Contents = new[]
            {
                new GeminiContent
                {
                    Parts = new[]
                    {
                        new GeminiPart { Text = ErrorAnalysisPromptBuilder.Build(error) },
                    },
                },
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = _options.Temperature,
                MaxOutputTokens = _options.MaxOutputTokens,
            },
        };

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"models/{_options.Model}:generateContent")
        {
            Content = JsonContent.Create(payload),
        };

        request.Headers.Add(ApiKeyHeaderName, _options.ApiKey);

        return request;
    }

    private async Task<HttpResponseMessage> SendRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Gemini API could not be reached.");

            throw new AiAnalysisException("Gemini API could not be reached.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Gemini API request timed out.");

            throw new AiAnalysisException("Gemini API request timed out.", exception);
        }
    }

    private static string ExtractAnalysisText(GeminiResponse? response)
    {
        var parts = response?.Candidates?.FirstOrDefault()?.Content?.Parts;

        if (parts is null)
        {
            throw new AiAnalysisException("Gemini returned a response without any content.");
        }

        var text = new StringBuilder();

        foreach (var part in parts.Where(part => !part.Thought && !string.IsNullOrWhiteSpace(part.Text)))
        {
            text.Append(part.Text);
        }

        var analysis = text.ToString().Trim();

        if (analysis.Length == 0)
        {
            throw new AiAnalysisException("Gemini returned an empty analysis.");
        }

        return analysis;
    }
}
