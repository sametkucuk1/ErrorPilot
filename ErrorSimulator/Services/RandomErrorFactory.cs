namespace ErrorSimulator.Services;

public static class RandomErrorFactory
{
    public static Exception CreateAndLog(ILogger logger, string source)
    {
       
        try
        {
            throw Create();
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Simulated error generated from {Source}: {ExceptionType}",
                source,
                exception.GetType().Name);

            return exception;
        }
    }

    public static Exception Create()
    {
        return Random.Shared.Next(0, 4) switch
        {
            0 => new NullReferenceException("Simulated null reference: object 'OrderContext' was not initialized."),
            1 => new DivideByZeroException("Simulated division by zero while calculating error rate."),
            2 => new InvalidOperationException("Simulated invalid operation: connection pool exhausted."),
            _ => new TimeoutException("Simulated timeout: downstream service did not respond in time."),
        };
    }
}
