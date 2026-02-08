# Petek Messenger

Kurumsal iç iletişim için tasarlanmış, Windows ekosistemiyle tam entegre, yüksek güvenlikli ve modern bir anlık mesajlaşma uygulaması.

## Genel Bakış

Petek Messenger, kurumsal kimlik yönetimi (Active Directory) ile uyumlu, denetlenebilir (Surveillance/DLP) ve kullanıcı dostu bir haberleşme altyapısı sağlar.

### Temel Özellikler

- **Windows AD Entegrasyonu**: LDAP/Kerberos ile mevcut oturumlarla otomatik giriş (SSO)
- **Gerçek Zamanlı Mesajlaşma**: SignalR tabanlı düşük gecikmeli mesaj iletimi
- **Birebir ve Grup Sohbeti**: Özel veya çoklu katılımcılı grup sohbetleri
- **Dosya Paylaşımı**: Sürükle-bırak ile her türlü dosya transferi
- **Ekran Görüntüsü Aracı**: Dahili ekran yakalama ve çizim araçları
- **Durum Yönetimi**: Uygun, Meşgul, Dışarıda, Çevrimdışı durumları
- **Surveillance Entegrasyonu**: DLP ve arşivleme servisleri için API/Webhook desteği
- **Karanlık/Aydınlık Mod**: Windows temasına uyumlu Fluent Design

## Mimari

```
┌─────────────────────────────────────────────────────────────────┐
│                      Petek.Desktop (WPF)                        │
│                     Windows Desktop Client                       │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ SignalR / HTTPS
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Petek.Server                               │
│                   ASP.NET Core 8 API                            │
│         ┌──────────────────┬──────────────────┐                 │
│         │    SignalR Hub   │    REST API      │                 │
│         └──────────────────┴──────────────────┘                 │
│                              │                                   │
│         ┌──────────────────┬──────────────────┐                 │
│         │    PostgreSQL    │      Redis       │                 │
│         │   (Ana Veritabanı)│    (Cache)      │                 │
│         └──────────────────┴──────────────────┘                 │
└─────────────────────────────────────────────────────────────────┘
```

## Teknoloji Yığını

| Katman | Teknoloji |
|--------|-----------|
| Desktop Client | WPF, .NET 8, CommunityToolkit.Mvvm |
| Backend API | ASP.NET Core 8, SignalR |
| Veritabanı | PostgreSQL 15+ |
| Cache | Redis 7+ |
| Authentication | Windows Negotiate (Kerberos/NTLM) |

## Proje Yapısı

```
Petek/
├── src/
│   ├── Petek.Desktop/          # WPF Windows masaüstü uygulaması
│   │   ├── Views/             # XAML görünümleri
│   │   ├── ViewModels/        # MVVM view modelleri
│   │   ├── Services/          # API ve SignalR istemcileri
│   │   ├── Themes/            # Fluent Design stilleri
│   │   └── Converters/        # XAML dönüştürücüleri
│   │
│   ├── Petek.Server/           # ASP.NET Core backend
│   │   ├── Controllers/       # REST API controller'ları
│   │   ├── Hubs/              # SignalR hub'ları
│   │   ├── Services/          # İş mantığı servisleri
│   │   └── Data/              # Entity Framework context ve entity'ler
│   │
│   └── Petek.Shared/           # Ortak modeller ve DTO'lar
│       ├── DTOs/              # Veri transfer objeleri
│       ├── Enums/             # Enum tanımları
│       └── Interfaces/        # Ortak interface'ler
│
├── docs/
│   └── PRD.md                 # Ürün Gereksinim Dokümanı
│
└── Petek.sln                   # Visual Studio solution dosyası
```

## Hızlı Başlangıç

### Gereksinimler

- .NET 8 SDK
- PostgreSQL 15+
- Redis 7+
- Windows 10/11 (Desktop client için)
- Visual Studio 2022 veya VS Code

### 1. Repository'yi Klonlayın

```bash
git clone https://github.com/your-org/petek.git
cd petek
```

### 2. Veritabanlarını Başlatın

```bash
# Docker ile (önerilen)
docker run -d --name petek-postgres -p 5432:5432 \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=petek \
  postgres:15

docker run -d --name petek-redis -p 6379:6379 redis:7-alpine
```

### 3. Sunucuyu Çalıştırın

```bash
cd src/Petek.Server
dotnet run
```

Sunucu varsayılan olarak `https://localhost:5001` adresinde çalışır.

### 4. Desktop İstemcisini Çalıştırın

```bash
cd src/Petek.Desktop
dotnet run
```

## API Dokümantasyonu

Sunucu çalıştırıldığında Swagger UI şu adreste erişilebilir:
- https://localhost:5001/swagger

## Konfigürasyon

Sunucu ayarları `src/Petek.Server/appsettings.json` dosyasından yapılandırılır:

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=petek;Username=postgres;Password=postgres",
    "Redis": "localhost:6379"
  },
  "FileStorage": {
    "Path": "./uploads",
    "MaxFileSizeBytes": 104857600
  },
  "Jwt": {
    "Secret": "your-super-secret-key-here",
    "ExpirationHours": 24
  }
}
```

## Kullanıcı Rolleri

| Rol | Yetkiler |
|-----|----------|
| Kullanıcı | Mesaj gönderme, dosya paylaşma, durum yönetimi |
| Moderatör | Grup sohbetlerini yönetme, kullanıcı ekleme/çıkarma |
| Sistem Yöneticisi | AD entegrasyonu, gözetim araçları, log denetimi |

## Lisans

Bu proje özel/kurumsal kullanım içindir.

## Destek

Teknik destek için sistem yöneticinizle iletişime geçin.
