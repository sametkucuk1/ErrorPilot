namespace ErrorPilotEngine.Services;

public interface IErrorWatermarkStore
{
    DateTimeOffset Current { get; }

    void Advance(DateTimeOffset timestamp);
}
