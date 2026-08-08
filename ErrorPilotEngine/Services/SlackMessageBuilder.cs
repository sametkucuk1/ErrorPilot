using ErrorPilotEngine.Models;

namespace ErrorPilotEngine.Services;

internal static class SlackMessageBuilder
{
    private const string UnknownValue = "bilinmiyor";
    private const string TruncationSuffix = "...";
    private const int MaxHeaderLength = 150;
    private const int MaxFieldLength = 2000;
    private const int MaxErrorMessageLength = 400;
    private const int MaxAnalysisLength = 2500;

    public static SlackMessage Build(AnalyzedError analyzedError)
    {
        var error = analyzedError.Error;
        var errorType = Fallback(error.Type);
        var errorMessage = Truncate(Fallback(error.Message), MaxErrorMessageLength);
        var analysis = Truncate(Fallback(analyzedError.Analysis), MaxAnalysisLength);

        return new SlackMessage
        {
            Text = Truncate($"[ErrorPilot] {errorType}: {errorMessage}", MaxHeaderLength),
            Blocks = new[]
            {
                new SlackBlock
                {
                    Type = "header",
                    Text = new SlackTextObject
                    {
                        Type = "plain_text",
                        Text = Truncate($"🚨 {errorType}", MaxHeaderLength),
                    },
                },
                new SlackBlock
                {
                    Type = "section",
                    Fields = BuildContextFields(error),
                },
                new SlackBlock
                {
                    Type = "section",
                    Text = CreateMarkdownText($"*Hata mesajı*\n{Escape(errorMessage)}"),
                },
                new SlackBlock
                {
                    Type = "section",
                    Text = CreateMarkdownText($"*AI önerisi*\n{Escape(analysis)}"),
                },
                new SlackBlock
                {
                    Type = "divider",
                },
            },
        };
    }

    private static IReadOnlyList<SlackTextObject> BuildContextFields(ErrorRecord error)
    {
        var fields = new List<SlackTextObject>
        {
            CreateField("Uygulama", Fallback(error.CloudRoleName)),
            CreateField("Zaman", error.Timestamp?.ToString("u") ?? UnknownValue),
        };

        if (!string.IsNullOrWhiteSpace(error.OperationName))
        {
            fields.Add(CreateField("Operasyon", error.OperationName));
        }

        if (!string.IsNullOrWhiteSpace(error.ProblemId))
        {
            fields.Add(CreateField("Konum", error.ProblemId));
        }

        return fields;
    }

    private static SlackTextObject CreateField(string label, string value)
    {
        return CreateMarkdownText(Truncate($"*{label}*\n{Escape(value)}", MaxFieldLength));
    }

    private static SlackTextObject CreateMarkdownText(string text)
    {
        return new SlackTextObject
        {
            Type = "mrkdwn",
            Text = text,
        };
    }

    private static string Escape(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    private static string Fallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? UnknownValue : value;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return string.Concat(
            value.AsSpan(0, maxLength - TruncationSuffix.Length),
            TruncationSuffix);
    }
}
