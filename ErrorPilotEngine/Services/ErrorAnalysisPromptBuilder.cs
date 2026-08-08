using System.Text;
using ErrorPilotEngine.Models;

namespace ErrorPilotEngine.Services;

public static class ErrorAnalysisPromptBuilder
{
    private const string UnknownValue = "bilinmiyor";

    public static string Build(ErrorRecord error)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine("Aşağıdaki .NET uygulama hatasını analiz et.");
        prompt.AppendLine();
        prompt.AppendLine($"Hata tipi: {Fallback(error.Type)}");
        prompt.AppendLine($"Hata mesajı: {Fallback(error.Message)}");

        if (!string.IsNullOrWhiteSpace(error.ProblemId))
        {
            prompt.AppendLine($"Hatanın oluştuğu konum: {error.ProblemId}");
        }

        if (!string.IsNullOrWhiteSpace(error.CloudRoleName))
        {
            prompt.AppendLine($"Uygulama: {error.CloudRoleName}");
        }

        prompt.AppendLine();
        prompt.AppendLine("Bu hatanın olası nedeni nedir ve nasıl çözülür?");
        prompt.AppendLine("Türkçe, kısa ve maddeler halinde cevap ver.");

        return prompt.ToString();
    }

    private static string Fallback(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? UnknownValue : value;
    }
}
