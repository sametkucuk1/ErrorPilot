namespace ErrorPilotEngine.Services;

public class AiRateLimitExceededException : AiAnalysisException
{
    public AiRateLimitExceededException(string message)
        : base(message)
    {
    }
}
