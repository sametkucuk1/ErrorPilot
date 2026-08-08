namespace ErrorPilotEngine.Services;

public class LogAnalyticsQueryException : Exception
{
    public LogAnalyticsQueryException(string message)
        : base(message)
    {
    }

    public LogAnalyticsQueryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
