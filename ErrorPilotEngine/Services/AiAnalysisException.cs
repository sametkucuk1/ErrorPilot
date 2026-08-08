namespace ErrorPilotEngine.Services;

public class AiAnalysisException : Exception
{
    public AiAnalysisException(string message)
        : base(message)
    {
    }

    public AiAnalysisException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
