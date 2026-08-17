# ErrorPilot

Uygulama hatalarını otomatik yakalayan, Google Gemini ile analiz ettiren ve sonucu Slack üzerinden ekibe bildiren uçtan uca bir izleme sistemi.

## Genel Bakış

ErrorPilot iki bağımsız .NET projesinden oluşuyor:

- **ErrorSimulator** — production ortamını taklit eden, rastgele aralıklarla dört farklı istisna tipi üreten bir Web API. Arka planda çalışan bir `BackgroundService`, on beş ile otuz saniye arasında rastgele bir süre bekleyip yüzde otuz olasılıkla bir hata fırlatıyor. Bu hatalar Application Insights SDK'sı üzerinden doğrudan Azure Log Analytics'e gönderiliyor.

- **ErrorPilotEngine** — asıl işi yapan servis. Azure Log Analytics'i sorgulayıp yeni hataları yakalıyor, her birini Google Gemini'ye göndererek olası neden ve çözüm önerisini Türkçe olarak ürettiriyor, sonucu Slack'e formatlı bir mesaj olarak iletiyor. Bunların hepsi arka planda çalışan bir zamanlayıcı ile otomatik gerçekleşiyor.

İki proje birbiriyle hiç doğrudan konuşmuyor; aralarındaki tek bağlantı, paylaşılan Azure Log Analytics çalışma alanı.

## Akış

```
ErrorSimulator (hata üretir)
        │
        ▼
Application Insights ──▶ Azure Log Analytics (AppExceptions tablosu)
        │
        ▼
ErrorPilotEngine — her 2 dakikada bir yeni hata var mı diye sorar
        │
        ▼
Google Gemini — hatayı analiz eder, Türkçe açıklama üretir
        │
        ▼
Slack — formatlı bir mesaj olarak ekibe düşer
```

ErrorPilotEngine, aynı hatayı iki kez işlememek için bir watermark mekanizması kullanıyor: her sorgudan sonra en son işlediği hatanın zamanını bellekte tutuyor ve bir sonraki sorguda sadece ondan sonrasını istiyor.

## Bileşenler

| Proje | Sorumluluk | Barındığı Yer |
|---|---|---|
| ErrorSimulator | Sahte hata üretimi, Application Insights'a telemetri gönderimi | Yerel geliştirme makinesi |
| ErrorPilotEngine | Log okuma, AI analizi, Slack bildirimi | Azure App Service |

## Uç Noktalar (ErrorPilotEngine)

- `GET /api/errors/latest` — Log Analytics'teki son 10 hatayı, analiz yapmadan döndürür.
- `GET /api/errors/analyzed` — yeni hataları çeker, Gemini ile analiz ettirir, Slack'e bildirir.

## Çalıştırma

Her iki proje de kendi klasöründen bağımsız olarak çalıştırılabilir:

```bash
dotnet run --project ErrorSimulator
dotnet run --project ErrorPilotEngine
```

ErrorPilotEngine'in çalışabilmesi için üç ayarın user-secrets ya da ortam değişkeni olarak tanımlanması gerekiyor:

```
LogAnalytics:WorkspaceId
Gemini:ApiKey
Slack:WebhookUrl
```

Bu değerlerin hiçbiri kod deposunda bulunmuyor.

## Dağıtım

`main` branch'ine yapılan her push, GitHub Actions üzerinden ErrorPilotEngine'i derleyip Azure App Service'e otomatik olarak deploy ediyor. Uygulama, Azure'daki Log Analytics'e erişirken herhangi bir şifre veya anahtar saklamak yerine sistem tarafından atanmış bir Managed Identity kullanıyor; bu kimliğe yalnızca ilgili çalışma alanında okuma yetkisi (Log Analytics Reader) veriliyor.

## Kullanılan Teknolojiler

.NET 8, ASP.NET Core Web API, Azure Log Analytics, Azure Application Insights, Azure App Service, Azure Managed Identity, Google Gemini API, Slack Incoming Webhooks, GitHub Actions.
