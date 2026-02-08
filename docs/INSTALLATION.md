# Petek Messenger Sunucu Kurulum Rehberi

Bu doküman, Petek Messenger sunucusunun kurulumu, yapılandırılması ve production ortamına deploy edilmesi için gerekli adımları içerir.

## Sistem Gereksinimleri

### Donanım (Minimum)

| Kaynak | Minimum | Önerilen |
|--------|---------|----------|
| CPU | 2 vCPU | 4+ vCPU |
| RAM | 4 GB | 8+ GB |
| Disk | 50 GB SSD | 100+ GB SSD |
| Ağ | 100 Mbps | 1 Gbps |

### Yazılım Gereksinimleri

| Bileşen | Versiyon | Açıklama |
|---------|----------|----------|
| İşletim Sistemi | Windows Server 2019/2022 veya Ubuntu 22.04+ | Linux önerilen |
| .NET Runtime | 8.0+ | ASP.NET Core Runtime |
| PostgreSQL | 15+ | Ana veritabanı |
| Redis | 7+ | Cache ve session yönetimi |
| Nginx/IIS | En son sürüm | Reverse proxy (opsiyonel) |

## Kurulum Adımları

### 1. .NET 8 Runtime Kurulumu

#### Ubuntu/Debian
```bash
# Microsoft paket deposunu ekle
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# .NET Runtime kurulumu
sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-8.0
```

#### Windows Server
```powershell
# winget ile kurulum
winget install Microsoft.DotNet.AspNetCore.8

# veya manuel indirme
# https://dotnet.microsoft.com/download/dotnet/8.0
```

### 2. PostgreSQL Kurulumu

#### Ubuntu/Debian
```bash
# PostgreSQL kurulumu
sudo apt-get install -y postgresql-15

# PostgreSQL servisini başlat
sudo systemctl start postgresql
sudo systemctl enable postgresql

# Veritabanı ve kullanıcı oluştur
sudo -u postgres psql << EOF
CREATE USER Petek_user WITH PASSWORD 'GucluSifre123!';
CREATE DATABASE Petek OWNER Petek_user;
GRANT ALL PRIVILEGES ON DATABASE Petek TO Petek_user;
EOF
```

#### Docker ile (Önerilen)
```bash
docker run -d \
  --name Petek-postgres \
  --restart unless-stopped \
  -p 5432:5432 \
  -e POSTGRES_USER=Petek_user \
  -e POSTGRES_PASSWORD=GucluSifre123! \
  -e POSTGRES_DB=Petek \
  -v Petek-postgres-data:/var/lib/postgresql/data \
  postgres:15-alpine
```

### 3. Redis Kurulumu

#### Ubuntu/Debian
```bash
# Redis kurulumu
sudo apt-get install -y redis-server

# Redis yapılandırması
sudo sed -i 's/bind 127.0.0.1/bind 0.0.0.0/' /etc/redis/redis.conf
sudo sed -i 's/# requirepass foobared/requirepass GucluRedisParola123!/' /etc/redis/redis.conf

# Servisi yeniden başlat
sudo systemctl restart redis-server
sudo systemctl enable redis-server
```

#### Docker ile (Önerilen)
```bash
docker run -d \
  --name Petek-redis \
  --restart unless-stopped \
  -p 6379:6379 \
  -v Petek-redis-data:/data \
  redis:7-alpine redis-server --requirepass GucluRedisParola123!
```

### 4. Uygulama Kurulumu

#### Kaynak Koddan Derleme
```bash
# Kaynak kodu klonla
git clone https://github.com/your-org/Petek.git
cd Petek

# Release build
dotnet publish src/Petek.Server/Petek.Server.csproj \
  -c Release \
  -o /opt/Petek-server
```

#### Uygulama Dizini Yapısı
```
/opt/Petek-server/
├── appsettings.json          # Ana yapılandırma
├── appsettings.Production.json  # Production ayarları
├── Petek.Server.dll           # Ana uygulama
├── uploads/                  # Dosya yükleme dizini
└── logs/                     # Log dosyaları
```

### 5. Yapılandırma

`/opt/Petek-server/appsettings.Production.json` dosyasını oluşturun:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Error"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=Petek;Username=Petek_user;Password=GucluSifre123!",
    "Redis": "localhost:6379,password=GucluRedisParola123!"
  },
  "Cors": {
    "Origins": [
      "https://Petek.sirketiniz.com"
    ]
  },
  "FileStorage": {
    "Path": "/opt/Petek-server/uploads",
    "MaxFileSizeBytes": 104857600
  },
  "Jwt": {
    "Secret": "minimum-32-karakter-uzunlugunda-guclu-bir-secret-key-kullanin",
    "Issuer": "PetekMessenger",
    "Audience": "PetekDesktop",
    "ExpirationHours": 24
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000"
      },
      "Https": {
        "Url": "https://0.0.0.0:5001",
        "Certificate": {
          "Path": "/etc/ssl/certs/Petek.pfx",
          "Password": "SertifikaSifresi"
        }
      }
    }
  }
}
```

### 6. Dosya İzinleri
```bash
# Uygulama kullanıcısı oluştur
sudo useradd -r -s /bin/false Petek

# Dizin sahipliğini ayarla
sudo chown -R Petek:Petek /opt/Petek-server
sudo chmod -R 750 /opt/Petek-server

# Upload dizini için yazma izni
sudo chmod 770 /opt/Petek-server/uploads
```

### 7. Systemd Service Oluşturma (Linux)

`/etc/systemd/system/Petek-server.service` dosyasını oluşturun:

```ini
[Unit]
Description=Petek Messenger Server
After=network.target postgresql.service redis.service

[Service]
Type=notify
User=Petek
Group=Petek
WorkingDirectory=/opt/Petek-server
ExecStart=/usr/bin/dotnet /opt/Petek-server/Petek.Server.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=Petek-server
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

# Güvenlik ayarları
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/opt/Petek-server/uploads /opt/Petek-server/logs

[Install]
WantedBy=multi-user.target
```

Servisi etkinleştirin:
```bash
sudo systemctl daemon-reload
sudo systemctl enable Petek-server
sudo systemctl start Petek-server
sudo systemctl status Petek-server
```

### 8. Windows Service Kurulumu (Windows Server)

```powershell
# NSSM ile Windows Service oluşturma
# NSSM'i indir: https://nssm.cc/download

nssm install PetekServer "C:\Program Files\dotnet\dotnet.exe" `
    "C:\PetekServer\Petek.Server.dll"
nssm set PetekServer AppDirectory "C:\PetekServer"
nssm set PetekServer AppEnvironmentExtra "ASPNETCORE_ENVIRONMENT=Production"
nssm set PetekServer DisplayName "Petek Messenger Server"
nssm set PetekServer Start SERVICE_AUTO_START

# Servisi başlat
nssm start PetekServer
```

## Reverse Proxy Yapılandırması

### Nginx (Önerilen)

`/etc/nginx/sites-available/Petek` dosyasını oluşturun:

```nginx
upstream Petek_server {
    server 127.0.0.1:5000;
    keepalive 32;
}

server {
    listen 80;
    server_name Petek.sirketiniz.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name Petek.sirketiniz.com;

    ssl_certificate /etc/ssl/certs/Petek.crt;
    ssl_certificate_key /etc/ssl/private/Petek.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256;
    ssl_prefer_server_ciphers off;

    # Dosya yükleme limiti
    client_max_body_size 100M;

    location / {
        proxy_pass http://Petek_server;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;

        # SignalR için timeout ayarları
        proxy_read_timeout 86400s;
        proxy_send_timeout 86400s;
    }

    # SignalR hub endpoint'i
    location /hubs/ {
        proxy_pass http://Petek_server;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 86400s;
        proxy_send_timeout 86400s;
    }
}
```

Etkinleştirin:
```bash
sudo ln -s /etc/nginx/sites-available/Petek /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### IIS (Windows)

1. IIS Manager'ı açın
2. "Application Request Routing" modülünü kurun
3. Site oluşturun ve Reverse Proxy yapılandırın
4. WebSocket desteğini etkinleştirin

## Active Directory Entegrasyonu

### Gereksinimler
- Sunucu, domain'e katılmış olmalı
- Servis hesabı için uygun SPN tanımlanmalı

### SPN Kayıt
```powershell
# Domain Controller'da çalıştırın
setspn -S HTTP/Petek.sirketiniz.com DOMAIN\Petek-service
setspn -S HTTP/Petek-server DOMAIN\Petek-service
```

### Keytab Oluşturma (Linux için)
```bash
# Domain Controller'da
ktpass /out Petek.keytab /princ HTTP/Petek.sirketiniz.com@DOMAIN.COM \
    /mapuser Petek-service /crypto ALL /pass * /ptype KRB5_NT_PRINCIPAL
```

## Güvenlik Önerileri

### Firewall Kuralları
```bash
# Sadece gerekli portları aç
sudo ufw allow 443/tcp    # HTTPS
sudo ufw allow 80/tcp     # HTTP (redirect için)
sudo ufw deny 5000/tcp    # Direkt erişimi engelle
sudo ufw deny 5001/tcp
sudo ufw enable
```

### SSL/TLS Sertifikası
```bash
# Let's Encrypt ile ücretsiz sertifika
sudo apt-get install certbot python3-certbot-nginx
sudo certbot --nginx -d Petek.sirketiniz.com
```

### Veritabanı Güvenliği
```sql
-- Sadece gerekli IP'lerden erişime izin ver
-- pg_hba.conf dosyasını düzenleyin
host    Petek    Petek_user    10.0.0.0/24    scram-sha-256
```

## Yedekleme

### Veritabanı Yedeği
```bash
#!/bin/bash
# /opt/Petek-backup/backup.sh

BACKUP_DIR="/opt/Petek-backup"
DATE=$(date +%Y%m%d_%H%M%S)

# PostgreSQL yedeği
pg_dump -U Petek_user -h localhost Petek | gzip > "$BACKUP_DIR/Petek_db_$DATE.sql.gz"

# Upload dosyaları yedeği
tar -czf "$BACKUP_DIR/Petek_uploads_$DATE.tar.gz" /opt/Petek-server/uploads

# 7 günden eski yedekleri sil
find "$BACKUP_DIR" -name "*.gz" -mtime +7 -delete
```

Cron ile zamanlayın:
```bash
# Her gün gece 02:00'de yedek al
0 2 * * * /opt/Petek-backup/backup.sh
```

## İzleme ve Loglama

### Log Konumları
- Uygulama logları: `/opt/Petek-server/logs/`
- Systemd logları: `journalctl -u Petek-server -f`
- Nginx erişim logları: `/var/log/nginx/access.log`

### Health Check Endpoint
```bash
# Sunucu durumunu kontrol et
curl -k https://localhost:5001/health
```

## Sorun Giderme

### Yaygın Sorunlar

| Sorun | Çözüm |
|-------|-------|
| Bağlantı hatası | Firewall kurallarını kontrol edin |
| Veritabanı hatası | PostgreSQL servisini ve bağlantı bilgilerini kontrol edin |
| SignalR bağlantı kopması | Nginx timeout ayarlarını artırın |
| Dosya yükleme hatası | Upload dizini izinlerini kontrol edin |

### Log Analizi
```bash
# Son hataları görüntüle
journalctl -u Petek-server --since "1 hour ago" | grep -i error

# Gerçek zamanlı log takibi
journalctl -u Petek-server -f
```

## Production Checklist

- [ ] .NET 8 Runtime kuruldu
- [ ] PostgreSQL kuruldu ve yapılandırıldı
- [ ] Redis kuruldu ve yapılandırıldı
- [ ] SSL sertifikası yapılandırıldı
- [ ] Reverse proxy (Nginx/IIS) yapılandırıldı
- [ ] Firewall kuralları uygulandı
- [ ] Yedekleme sistemi kuruldu
- [ ] Log rotasyonu yapılandırıldı
- [ ] AD entegrasyonu test edildi
- [ ] Health monitoring kuruldu
- [ ] appsettings.Production.json güvenli şekilde yapılandırıldı

## Destek

Teknik destek için sistem yöneticinizle iletişime geçin.
