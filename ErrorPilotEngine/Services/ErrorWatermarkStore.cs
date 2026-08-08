namespace ErrorPilotEngine.Services;

public class ErrorWatermarkStore : IErrorWatermarkStore
{
    private readonly object _gate = new();

    private DateTimeOffset _current = DateTimeOffset.UtcNow;

    public DateTimeOffset Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    public void Advance(DateTimeOffset timestamp)
    {
        lock (_gate)
        {
            if (timestamp > _current)
            {
                _current = timestamp;
            }
        }
    }
}
