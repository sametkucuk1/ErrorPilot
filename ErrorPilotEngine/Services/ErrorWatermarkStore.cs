namespace ErrorPilotEngine.Services;

// Aynı hatanın iki kez analiz edilip Slack'e iki kez düşmesini engelleyen basit bir işaretçi.
// Zamanlayıcı ile HTTP isteği aynı anda sorgu çalıştırabildiği için erişim kilitle korunuyor.
// Değer bellekte tutuluyor; sınırları için README'deki "Bilinen Sınırlamalar" bölümüne bakın.
public class ErrorWatermarkStore : IErrorWatermarkStore
{
    private readonly object _gate = new();

    // Başlangıç değeri uygulamanın açıldığı an: açılıştan önceki eski hatalar işlenmiyor.
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
            // İşaretçi yalnızca ileri gider; geç gelen bir kayıt onu geri alamaz.
            if (timestamp > _current)
            {
                _current = timestamp;
            }
        }
    }
}
