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

## Uç Noktalar

**ErrorPilotEngine**

- `GET /api/errors/latest` — Log Analytics'teki son 10 hatayı, analiz yapmadan döndürür.
- `GET /api/errors/analyzed` — yeni hataları çeker, Gemini ile analiz ettirir, Slack'e bildirir. Arka plandaki zamanlayıcı ile aynı işi yapar; işlenen hatalar işaretlendiği için arka arkaya iki çağrının ikincisi boş rapor döndürür.

**ErrorSimulator**

- `GET /api/errors/trigger` — zamanlayıcıyı beklemeden anında rastgele bir hata üretir. Demo sırasında zincirin tamamını tetiklemek için kullanılıyor.

Her iki projede de Swagger arayüzü yalnızca Development ortamında açık; Azure'a deploy edilen sürümde `/swagger` kapalıdır, uç noktalara doğrudan istek atılması gerekir.

## Çalıştırma

Çözüm dosyası kök dizinde: `ErrorPilot.sln`. Projeler birbirine bağlı olmadığı için ayrı ayrı da çalıştırılabilir:

```bash
dotnet run --project ErrorSimulator
dotnet run --project ErrorPilotEngine
```

### Gerekli ayarlar

Hiçbir gizli değer kod deposunda tutulmuyor; hepsi user-secrets ya da ortam değişkeni olarak veriliyor. `appsettings.json` içindeki karşılıkları boş bırakılmış durumda.

ErrorPilotEngine için:

```
LogAnalytics:WorkspaceId
Gemini:ApiKey
Slack:WebhookUrl
```

ErrorSimulator için:

```
ApplicationInsights:ConnectionString
```

Örnek:

```bash
dotnet user-secrets --project ErrorPilotEngine set "Gemini:ApiKey" "<anahtar>"
```

Bu ayarların eksik olması iki projede farklı sonuç veriyor. ErrorPilotEngine, ayarları başlangıçta doğruladığı için eksik değerle **hiç açılmaz** ve hangi ayarın eksik olduğunu söyleyen bir hata verir. ErrorSimulator ise sorunsuz açılır, hata üretmeye de devam eder; ancak bağlantı dizesi olmadan telemetri Azure'a ulaşmaz. Bu sessiz durumu fark etmek zor olduğu için uygulama başlangıçta bir uyarı logu yazıyor.

Azure'a deploy edilen ErrorPilotEngine, Log Analytics'e erişirken Managed Identity kullandığından orada yalnızca `Gemini:ApiKey` ve `Slack:WebhookUrl` uygulama ayarı olarak tanımlanıyor.

## Dağıtım

`main` branch'ine yapılan her push, GitHub Actions üzerinden ErrorPilotEngine'i derleyip Azure App Service'e otomatik olarak deploy ediyor. Uygulama, Azure'daki Log Analytics'e erişirken herhangi bir şifre veya anahtar saklamak yerine sistem tarafından atanmış bir Managed Identity kullanıyor; bu kimliğe yalnızca ilgili çalışma alanında okuma yetkisi (Log Analytics Reader) veriliyor.

## Bilinen Sınırlamalar

Demo kapsamında bilinçli olarak basit tutulan ya da dışarıda bırakılan noktalar aşağıda toplandı.

- **Watermark bellekte tutuldu.** Kalıcı bir depo yerine bellek tercih edildiği için uygulama her yeniden başladığında işaretçi o anki zamana sıfırlanıyor ve yeniden başlatma sırasında oluşan hatalar atlanabiliyor. Kalıcı bir depoya (ör. tablo ya da blob) taşınarak giderilebilir.
- **Tek instance varsayımıyla yazıldı.** App Service birden fazla instance'a ölçeklendiğinde her instance kendi işaretçisini tutacağı için aynı hata Slack'e birden fazla kez düşebilir.
- **Sorguda kesin büyüktür (`>`) karşılaştırması kullanıldı.** İşaretçiyle tam olarak aynı zaman damgasına sahip ikinci bir hata bir sonraki turda çekilmiyor. Zaman damgaları milisaniye çözünürlüğünde olduğu için pratikte nadir görülen bir durum olarak değerlendirildi.
- **Uç noktalara kimlik doğrulaması eklenmedi.** Adresi bilen herkes `/api/errors/analyzed` çağırarak Gemini kotasını tüketebilir veya Slack kanalına mesaj düşürebilir. Ödev kapsamında yeterli görüldü; gerçek bir kullanımda en azından bir API anahtarı kontrolü eklenmeli.
- **Kota sınırına takılınca yeniden deneme yapılmadı.** Gemini'den HTTP 429 alındığında o turdaki kalan hatalar "atlandı" olarak raporlanıp bir sonraki tura bırakılıyor.
- **Otomatik test yazılmadı.** Doğrulama, iki proje birlikte çalıştırılıp Slack'e düşen mesajlar gözlenerek yapıldı.

## Kullanılan Teknolojiler

.NET 8, ASP.NET Core Web API, Azure Log Analytics, Azure Application Insights, Azure App Service, Azure Managed Identity, Google Gemini API, Slack Incoming Webhooks, GitHub Actions.
