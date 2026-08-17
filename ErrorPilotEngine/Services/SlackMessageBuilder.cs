using System.Text.RegularExpressions;
using ErrorPilotEngine.Models;

namespace ErrorPilotEngine.Services;

internal static class SlackMessageBuilder
{
    // Gemini standart Markdown üretiyor: kalın yazı için "**metin**", madde başı için "* ".
    // Slack ise mrkdwn kullandığından bu işaretleri biçimlendirmeden, olduğu gibi yazdırıyor.
    // Mesajın okunabilir kalması için gönderim öncesi Slack'in beklediği biçime çeviriyoruz.
    private static readonly Regex BoldPattern = new(@"\*\*(.+?)\*\*", RegexOptions.Compiled);

    private static readonly Regex BulletPattern = new(
        @"^[ \t]*[*-][ \t]+",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private const string UnknownValue = "bilinmiyor";
    private const string TruncationSuffix = "...";

    // Slack Block Kit'in karakter sınırları. Aşılırsa webhook mesajı reddediyor,
    // bu yüzden metinleri gönderim öncesi kısaltıyoruz.
    private const int MaxHeaderLength = 150;
    private const int MaxFieldLength = 2000;
    private const int MaxErrorMessageLength = 400;
    private const int MaxAnalysisLength = 2500;

    public static SlackMessage Build(AnalyzedError analyzedError)
    {
        var error = analyzedError.Error;
        var errorType = Fallback(error.Type);
        var errorMessage = Truncate(Fallback(error.Message), MaxErrorMessageLength);
        var analysis = Truncate(
            ToSlackMarkdown(Fallback(analyzedError.Analysis)),
            MaxAnalysisLength);

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

    private static string ToSlackMarkdown(string value)
    {
        var converted = BoldPattern.Replace(value, "*$1*");

        return BulletPattern.Replace(converted, "• ");
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
