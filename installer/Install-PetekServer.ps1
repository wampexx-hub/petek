#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Petek Messenger Server - Windows Server Interaktif Kurulum Scripti
.DESCRIPTION
    Bu script, Petek Messenger sunucusunu Windows Server uzerine kurar.
    Gerekli bilgileri interaktif olarak sorar ve kurulum sonunda
    istemcilerin baglanmasi icin gereken bilgileri gosterir.
.NOTES
    Yonetici olarak calistirin.
#>

param(
    [switch]$Silent,
    [string]$ConfigFile
)

$ErrorActionPreference = "Stop"
$script:InstallPath = "C:\PetekServer"
$script:Port = 5000
$script:DbType = "SQLite"
$script:AdminUser = "admin"
$script:AdminPassword = ""
$script:ServerName = $env:COMPUTERNAME
$script:UseSSL = $false
$script:JwtSecret = ""
$script:MaxFileSize = 100

# ============================================================
# Yardimci Fonksiyonlar
# ============================================================

function Write-Banner {
    Clear-Host
    Write-Host ""
    Write-Host "  =============================================" -ForegroundColor Cyan
    Write-Host "     PETEK MESSENGER SERVER KURULUMU v1.1.0" -ForegroundColor Yellow
    Write-Host "  =============================================" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Step {
    param([string]$Step, [string]$Message)
    Write-Host "  [$Step] " -ForegroundColor Green -NoNewline
    Write-Host $Message
}

function Write-Info {
    param([string]$Message)
    Write-Host "  [BILGI] " -ForegroundColor Cyan -NoNewline
    Write-Host $Message
}

function Write-Warn {
    param([string]$Message)
    Write-Host "  [UYARI] " -ForegroundColor Yellow -NoNewline
    Write-Host $Message
}

function Write-Err {
    param([string]$Message)
    Write-Host "  [HATA] " -ForegroundColor Red -NoNewline
    Write-Host $Message
}

function Read-UserInput {
    param(
        [string]$Prompt,
        [string]$Default = "",
        [switch]$Required,
        [switch]$IsPassword
    )

    $defaultText = if ($Default) { " [$Default]" } else { "" }

    while ($true) {
        Write-Host "  $Prompt$defaultText`: " -ForegroundColor White -NoNewline

        if ($IsPassword) {
            $secureInput = Read-Host -AsSecureString
            $input = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
                [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureInput))
        }
        else {
            $input = Read-Host
        }

        if ([string]::IsNullOrWhiteSpace($input)) {
            if ($Default) { return $Default }
            if ($Required) {
                Write-Warn "Bu alan zorunludur."
                continue
            }
            return ""
        }
        return $input.Trim()
    }
}

function Read-Choice {
    param(
        [string]$Prompt,
        [string[]]$Options,
        [int]$Default = 1
    )

    Write-Host ""
    Write-Host "  $Prompt" -ForegroundColor White
    for ($i = 0; $i -lt $Options.Count; $i++) {
        $marker = if ($i + 1 -eq $Default) { " (*)" } else { "" }
        Write-Host "    $($i + 1). $($Options[$i])$marker" -ForegroundColor Gray
    }

    while ($true) {
        Write-Host "  Seciminiz [$Default]: " -ForegroundColor White -NoNewline
        $input = Read-Host

        if ([string]::IsNullOrWhiteSpace($input)) {
            return $Default
        }

        if ($input -match '^\d+$') {
            $num = [int]$input
            if ($num -ge 1 -and $num -le $Options.Count) {
                return $num
            }
        }
        Write-Warn "Gecersiz secim. 1 ile $($Options.Count) arasinda bir sayi girin."
    }
}

function Read-YesNo {
    param(
        [string]$Prompt,
        [bool]$Default = $true
    )

    $defaultText = if ($Default) { "E/h" } else { "e/H" }

    while ($true) {
        Write-Host "  $Prompt [$defaultText]: " -ForegroundColor White -NoNewline
        $input = Read-Host

        if ([string]::IsNullOrWhiteSpace($input)) { return $Default }
        if ($input -match '^[eEyY]') { return $true }
        if ($input -match '^[hHnN]') { return $false }
        Write-Warn "Lutfen E (evet) veya H (hayir) girin."
    }
}

function Generate-SecureKey {
    param([int]$Length = 64)
    $bytes = New-Object byte[] $Length
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($bytes)
    return [Convert]::ToBase64String($bytes)
}

function Get-ServerIPAddresses {
    $ips = @()
    try {
        $adapters = Get-NetIPAddress -AddressFamily IPv4 -Type Unicast |
            Where-Object { $_.IPAddress -ne "127.0.0.1" -and $_.PrefixOrigin -ne "WellKnown" }
        foreach ($adapter in $adapters) {
            $ips += $adapter.IPAddress
        }
    }
    catch {
        try {
            $adapters = [System.Net.NetworkInformation.NetworkInterface]::GetAllNetworkInterfaces() |
                Where-Object { $_.OperationalStatus -eq 'Up' }
            foreach ($adapter in $adapters) {
                $props = $adapter.GetIPProperties()
                foreach ($addr in $props.UnicastAddresses) {
                    if ($addr.Address.AddressFamily -eq 'InterNetwork' -and $addr.Address.ToString() -ne '127.0.0.1') {
                        $ips += $addr.Address.ToString()
                    }
                }
            }
        }
        catch { }
    }
    if ($ips.Count -eq 0) { $ips += "127.0.0.1" }
    return $ips
}

function Test-DotNetInstalled {
    try {
        $output = & dotnet --list-runtimes 2>&1
        $hasAspNet8 = $output | Where-Object { $_ -match "Microsoft\.AspNetCore\.App 8\." }
        return $null -ne $hasAspNet8
    }
    catch {
        return $false
    }
}

# ============================================================
# ADIM 1: Hosgeldiniz ve Gereksinimler
# ============================================================

function Step-Welcome {
    Write-Banner
    Write-Host "  Petek Messenger sunucusunu bu bilgisayara kuracaksiniz." -ForegroundColor Gray
    Write-Host "  Kurulum sirasinda asagidaki bilgiler sorulacaktir:" -ForegroundColor Gray
    Write-Host ""
    Write-Host "    - Kurulum dizini" -ForegroundColor Gray
    Write-Host "    - Sunucu portu" -ForegroundColor Gray
    Write-Host "    - Veritabani secimi" -ForegroundColor Gray
    Write-Host "    - Yonetici hesabi bilgileri" -ForegroundColor Gray
    Write-Host "    - SSL/HTTPS ayarlari" -ForegroundColor Gray
    Write-Host ""

    Write-Step "1/7" "Sistem gereksinimleri kontrol ediliyor..."
    Write-Host ""

    # .NET kontrolu
    $dotnetOk = Test-DotNetInstalled
    if ($dotnetOk) {
        Write-Info ".NET 8 ASP.NET Core Runtime bulundu."
    }
    else {
        Write-Err ".NET 8 ASP.NET Core Runtime bulunamadi!"
        Write-Host ""
        Write-Host "  .NET 8 Runtime'i indirip kurun:" -ForegroundColor Yellow
        Write-Host "  https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  veya PowerShell ile:" -ForegroundColor Yellow
        Write-Host "  winget install Microsoft.DotNet.AspNetCore.8" -ForegroundColor Cyan
        Write-Host ""

        if (-not (Read-YesNo "Kuruluma devam etmek istiyor musunuz?" $false)) {
            Write-Host ""
            Write-Host "  Kurulum iptal edildi." -ForegroundColor Yellow
            exit 0
        }
    }

    # OS kontrolu
    $os = Get-CimInstance Win32_OperatingSystem
    Write-Info "Isletim sistemi: $($os.Caption) ($($os.Version))"

    # RAM kontrolu
    $totalRamGB = [math]::Round($os.TotalVisibleMemorySize / 1MB, 1)
    if ($totalRamGB -lt 4) {
        Write-Warn "Sistem RAM'i ($totalRamGB GB) minimum gereksinimin altinda (4 GB)."
    }
    else {
        Write-Info "Sistem RAM: $totalRamGB GB"
    }

    Write-Host ""
    if (-not (Read-YesNo "Kuruluma baslamak istiyor musunuz?")) {
        Write-Host "  Kurulum iptal edildi." -ForegroundColor Yellow
        exit 0
    }
}

# ============================================================
# ADIM 2: Kurulum Dizini
# ============================================================

function Step-InstallPath {
    Write-Host ""
    Write-Step "2/7" "Kurulum dizini"
    Write-Host ""

    $script:InstallPath = Read-UserInput "Kurulum dizini" "C:\PetekServer"

    if (Test-Path $script:InstallPath) {
        Write-Warn "Bu dizin zaten mevcut."
        if (-not (Read-YesNo "Mevcut dosyalarin uzerine yazilsin mi?" $false)) {
            $script:InstallPath = Read-UserInput "Alternatif kurulum dizini" -Required
        }
    }
}

# ============================================================
# ADIM 3: Sunucu Ayarlari
# ============================================================

function Step-ServerConfig {
    Write-Host ""
    Write-Step "3/7" "Sunucu ayarlari"
    Write-Host ""

    $script:Port = [int](Read-UserInput "Sunucu portu" "5000")

    # Port kullanim kontrolu
    $portInUse = Get-NetTCPConnection -LocalPort $script:Port -ErrorAction SilentlyContinue
    if ($portInUse) {
        Write-Warn "$($script:Port) portu baska bir uygulama tarafindan kullaniliyor!"
        $script:Port = [int](Read-UserInput "Alternatif port" "5050" -Required)
    }

    # SSL
    Write-Host ""
    $script:UseSSL = Read-YesNo "HTTPS/SSL etkinlestirilsin mi? (Sertifika gerektirir)" $false

    if ($script:UseSSL) {
        $script:SSLPort = [int](Read-UserInput "HTTPS portu" "5001")
        $script:SSLCertPath = Read-UserInput "PFX sertifika dosya yolu" -Required
        $script:SSLCertPassword = Read-UserInput "Sertifika sifresi" -IsPassword -Required

        if (-not (Test-Path $script:SSLCertPath)) {
            Write-Warn "Sertifika dosyasi bulunamadi: $($script:SSLCertPath)"
            Write-Info "Kurulumdan sonra sertifikayi belirtilen yola kopyalayin."
        }
    }

    # Maks dosya boyutu
    Write-Host ""
    $script:MaxFileSize = [int](Read-UserInput "Maksimum dosya boyutu (MB)" "100")
}

# ============================================================
# ADIM 4: Veritabani
# ============================================================

function Step-Database {
    Write-Host ""
    Write-Step "4/7" "Veritabani ayarlari"

    $dbChoice = Read-Choice "Veritabani secin:" @(
        "SQLite (Kolay kurulum, kucuk/orta olcek)",
        "PostgreSQL (Buyuk olcek, yuksek performans)"
    ) 1

    if ($dbChoice -eq 1) {
        $script:DbType = "SQLite"
        $script:DbConnectionString = "Data Source=$($script:InstallPath)\petek.db"
        Write-Info "SQLite secildi. Ek kurulum gerekmez."
    }
    else {
        $script:DbType = "PostgreSQL"
        Write-Host ""
        $pgHost = Read-UserInput "PostgreSQL sunucu adresi" "localhost"
        $pgPort = Read-UserInput "PostgreSQL portu" "5432"
        $pgDb = Read-UserInput "Veritabani adi" "petek"
        $pgUser = Read-UserInput "Kullanici adi" "petek_user"
        $pgPass = Read-UserInput "Sifre" -IsPassword -Required

        $script:DbConnectionString = "Host=$pgHost;Port=$pgPort;Database=$pgDb;Username=$pgUser;Password=$pgPass"
        Write-Info "PostgreSQL baglanti bilgileri kaydedildi."
    }
}

# ============================================================
# ADIM 5: Yonetici Hesabi
# ============================================================

function Step-AdminAccount {
    Write-Host ""
    Write-Step "5/7" "Yonetici hesabi"
    Write-Host ""

    $script:AdminUser = Read-UserInput "Yonetici kullanici adi" "admin" -Required

    while ($true) {
        $script:AdminPassword = Read-UserInput "Yonetici sifresi (min 8 karakter)" -IsPassword -Required

        if ($script:AdminPassword.Length -lt 8) {
            Write-Warn "Sifre en az 8 karakter olmalidir."
            continue
        }

        $confirmPass = Read-UserInput "Sifreyi tekrar girin" -IsPassword -Required
        if ($script:AdminPassword -ne $confirmPass) {
            Write-Warn "Sifreler eslesmiyor. Tekrar deneyin."
            continue
        }
        break
    }

    Write-Info "Yonetici hesabi: $($script:AdminUser)"
}

# ============================================================
# ADIM 6: Ek Ayarlar
# ============================================================

function Step-AdditionalConfig {
    Write-Host ""
    Write-Step "6/7" "Ek ayarlar"
    Write-Host ""

    # Windows Auth
    $script:UseWindowsAuth = Read-YesNo "Windows/Active Directory kimlik dogrulamasi etkinlestirilsin mi?" $true

    # Firewall
    $script:OpenFirewall = Read-YesNo "Windows Firewall'da port acilsin mi?" $true

    # Windows Service
    $script:InstallAsService = Read-YesNo "Windows Servisi olarak kurulsun mu? (Otomatik baslatma)" $true

    # JWT Secret otomatik olustur
    $script:JwtSecret = Generate-SecureKey 48
}

# ============================================================
# ADIM 7: Kurulum
# ============================================================

function Step-Install {
    Write-Host ""
    Write-Step "7/7" "Kurulum baslatiliyor..."
    Write-Host ""

    # Dizin olustur
    Write-Info "Kurulum dizini olusturuluyor: $($script:InstallPath)"
    New-Item -ItemType Directory -Force -Path $script:InstallPath | Out-Null
    New-Item -ItemType Directory -Force -Path "$($script:InstallPath)\uploads" | Out-Null
    New-Item -ItemType Directory -Force -Path "$($script:InstallPath)\logs" | Out-Null

    # Uygulama dosyalarini kopyala
    $sourcePath = Split-Path -Parent $PSScriptRoot
    $publishPath = Join-Path $sourcePath "publish\server"

    if (-not (Test-Path $publishPath)) {
        # Publish dizini yoksa, mevcut dizinden kopyala
        $serverBinPath = Join-Path $sourcePath "src\Petek.Server\bin\Release\net8.0\publish"
        if (-not (Test-Path $serverBinPath)) {
            $serverBinPath = Join-Path $sourcePath "src\Petek.Server\bin\Release\net8.0"
        }
        if (-not (Test-Path $serverBinPath)) {
            # Script'in kendi bulundugu dizindeki dosyalari kullan
            $scriptDir = $PSScriptRoot
            $serverBinPath = Join-Path (Split-Path $scriptDir) "publish\server"
        }
        $publishPath = $serverBinPath
    }

    if (Test-Path $publishPath) {
        Write-Info "Uygulama dosyalari kopyalaniyor..."
        Copy-Item -Path "$publishPath\*" -Destination $script:InstallPath -Recurse -Force
    }
    else {
        Write-Warn "Derlenmi uygulama dosyalari bulunamadi."
        Write-Info "Once asagidaki komutu calistirin:"
        Write-Host "  dotnet publish src\Petek.Server\Petek.Server.csproj -c Release -o publish\server" -ForegroundColor Cyan
        Write-Host ""
        Write-Info "veya publish edilmis dosyalari su dizine kopyalayin:"
        Write-Host "  $($script:InstallPath)\" -ForegroundColor Cyan
    }

    # appsettings.json olustur
    Write-Info "Yapilandirma dosyasi olusturuluyor..."
    $kestrelConfig = @{
        Endpoints = @{
            Http = @{
                Url = "http://0.0.0.0:$($script:Port)"
            }
        }
    }

    if ($script:UseSSL) {
        $kestrelConfig.Endpoints.Https = @{
            Url = "https://0.0.0.0:$($script:SSLPort)"
            Certificate = @{
                Path = $script:SSLCertPath
                Password = $script:SSLCertPassword
            }
        }
    }

    $serverIPs = Get-ServerIPAddresses
    $corsOrigins = @("http://localhost:$($script:Port)")
    foreach ($ip in $serverIPs) {
        $corsOrigins += "http://${ip}:$($script:Port)"
        if ($script:UseSSL) {
            $corsOrigins += "https://${ip}:$($script:SSLPort)"
        }
    }

    $config = @{
        Logging = @{
            LogLevel = @{
                Default = "Information"
                "Microsoft.AspNetCore" = "Warning"
                "Microsoft.EntityFrameworkCore" = "Warning"
            }
        }
        AllowedHosts = "*"
        Database = @{
            Type = $script:DbType
        }
        ConnectionStrings = @{
            SQLite = "Data Source=petek.db"
            PostgreSQL = ""
            Redis = "localhost:6379"
        }
        SSO = @{
            Enabled = $script:UseWindowsAuth
            WindowsAuth = $script:UseWindowsAuth
        }
        Cors = @{
            Origins = $corsOrigins
        }
        FileStorage = @{
            Path = "$($script:InstallPath)\uploads"
            MaxFileSizeBytes = $script:MaxFileSize * 1024 * 1024
        }
        Jwt = @{
            Secret = $script:JwtSecret
            Issuer = "PetekMessenger"
            Audience = "PetekDesktop"
            ExpirationHours = 24
        }
        Kestrel = $kestrelConfig
        AdminSetup = @{
            Username = $script:AdminUser
            Password = $script:AdminPassword
        }
    }

    if ($script:DbType -eq "SQLite") {
        $config.ConnectionStrings.SQLite = $script:DbConnectionString
    }
    else {
        $config.ConnectionStrings.PostgreSQL = $script:DbConnectionString
    }

    $configJson = $config | ConvertTo-Json -Depth 10
    $configPath = Join-Path $script:InstallPath "appsettings.json"
    Set-Content -Path $configPath -Value $configJson -Encoding UTF8
    Write-Info "appsettings.json olusturuldu."

    # Firewall kurali
    if ($script:OpenFirewall) {
        Write-Info "Firewall kurali ekleniyor..."
        try {
            # Onceki kurallari temizle
            netsh advfirewall firewall delete rule name="Petek Messenger Server" >$null 2>&1

            netsh advfirewall firewall add rule `
                name="Petek Messenger Server" `
                dir=in action=allow protocol=tcp `
                localport=$($script:Port) `
                description="Petek Messenger Server - HTTP" | Out-Null

            if ($script:UseSSL) {
                netsh advfirewall firewall add rule `
                    name="Petek Messenger Server HTTPS" `
                    dir=in action=allow protocol=tcp `
                    localport=$($script:SSLPort) `
                    description="Petek Messenger Server - HTTPS" | Out-Null
            }
            Write-Info "Firewall kurallari eklendi."
        }
        catch {
            Write-Warn "Firewall kurali eklenemedi: $($_.Exception.Message)"
        }
    }

    # Windows Service kurulumu
    if ($script:InstallAsService) {
        Write-Info "Windows Servisi olusturuluyor..."
        try {
            $exePath = Join-Path $script:InstallPath "Petek.Server.exe"

            # Mevcut servisi kaldir
            $existingService = Get-Service -Name "PetekServer" -ErrorAction SilentlyContinue
            if ($existingService) {
                Stop-Service -Name "PetekServer" -Force -ErrorAction SilentlyContinue
                sc.exe delete PetekServer | Out-Null
                Start-Sleep -Seconds 2
            }

            # Yeni servis olustur
            New-Service -Name "PetekServer" `
                -BinaryPathName "`"$exePath`"" `
                -DisplayName "Petek Messenger Server" `
                -Description "Petek Messenger - Kurumsal mesajlasma sunucusu" `
                -StartupType Automatic | Out-Null

            # Servisi baslat
            Start-Service -Name "PetekServer" -ErrorAction SilentlyContinue
            Write-Info "PetekServer servisi olusturuldu ve baslatildi."
        }
        catch {
            Write-Warn "Servis olusturma hatasi: $($_.Exception.Message)"
            Write-Info "Servisi manuel olarak olusturabilirsiniz:"
            Write-Host "  sc.exe create PetekServer binPath= `"$exePath`" start= auto" -ForegroundColor Cyan
        }
    }

    # Upload dizini izinleri
    Write-Info "Dizin izinleri ayarlaniyor..."
    $acl = Get-Acl "$($script:InstallPath)\uploads"
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        "NETWORK SERVICE", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.AddAccessRule($rule)
    try {
        Set-Acl "$($script:InstallPath)\uploads" $acl
    }
    catch {
        Write-Warn "Izin ayarlama hatasi (onemli degil): $($_.Exception.Message)"
    }
}

# ============================================================
# SONUC: Baglanti Bilgileri
# ============================================================

function Show-ConnectionInfo {
    $serverIPs = Get-ServerIPAddresses

    Write-Host ""
    Write-Host "  =============================================" -ForegroundColor Green
    Write-Host "       KURULUM BASARIYLA TAMAMLANDI!" -ForegroundColor Green
    Write-Host "  =============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "  -----------------------------------------------" -ForegroundColor Cyan
    Write-Host "   SUNUCU BAGLANTI BILGILERI" -ForegroundColor Cyan
    Write-Host "  -----------------------------------------------" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  Sunucu Adi       : $($script:ServerName)" -ForegroundColor White
    Write-Host "  Kurulum Dizini   : $($script:InstallPath)" -ForegroundColor White
    Write-Host "  Veritabani       : $($script:DbType)" -ForegroundColor White
    Write-Host "  Windows Auth     : $(if ($script:UseWindowsAuth) {'Etkin'} else {'Devre Disi'})" -ForegroundColor White
    Write-Host ""
    Write-Host "  -----------------------------------------------" -ForegroundColor Yellow
    Write-Host "   ISTEMCI BAGLANTI ADRESLERI" -ForegroundColor Yellow
    Write-Host "  -----------------------------------------------" -ForegroundColor Yellow
    Write-Host ""

    foreach ($ip in $serverIPs) {
        Write-Host "    http://${ip}:$($script:Port)" -ForegroundColor Cyan
        if ($script:UseSSL) {
            Write-Host "    https://${ip}:$($script:SSLPort)" -ForegroundColor Cyan
        }
    }

    Write-Host ""
    Write-Host "    http://$($script:ServerName):$($script:Port)" -ForegroundColor Cyan
    if ($script:UseSSL) {
        Write-Host "    https://$($script:ServerName):$($script:SSLPort)" -ForegroundColor Cyan
    }

    Write-Host ""
    Write-Host "  -----------------------------------------------" -ForegroundColor Yellow
    Write-Host "   ONEMLI ENDPOINTLER" -ForegroundColor Yellow
    Write-Host "  -----------------------------------------------" -ForegroundColor Yellow
    Write-Host ""
    $proto = if ($script:UseSSL) { "https" } else { "http" }
    $port = if ($script:UseSSL) { $script:SSLPort } else { $script:Port }
    $baseUrl = "${proto}://$($serverIPs[0]):${port}"

    Write-Host "    API          : $baseUrl/api" -ForegroundColor White
    Write-Host "    SignalR Hub  : $baseUrl/hubs/message" -ForegroundColor White
    Write-Host "    Admin Panel  : $baseUrl/admin" -ForegroundColor White
    Write-Host "    Saglik       : $baseUrl/health" -ForegroundColor White
    Write-Host ""
    Write-Host "  -----------------------------------------------" -ForegroundColor Yellow
    Write-Host "   YONETICI HESABI" -ForegroundColor Yellow
    Write-Host "  -----------------------------------------------" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "    Kullanici    : $($script:AdminUser)" -ForegroundColor White
    Write-Host "    Sifre        : (kurulumda belirlediginiz sifre)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  -----------------------------------------------" -ForegroundColor Yellow
    Write-Host "   ISTEMCI YAPILANDIRMASI" -ForegroundColor Yellow
    Write-Host "  -----------------------------------------------" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "    Petek Desktop uygulamasinda:" -ForegroundColor Gray
    Write-Host "    Giris ekraninda sunucu adresini girin:" -ForegroundColor Gray
    Write-Host ""
    Write-Host "    $baseUrl" -ForegroundColor Green
    Write-Host ""

    if ($script:InstallAsService) {
        Write-Host "  -----------------------------------------------" -ForegroundColor Yellow
        Write-Host "   SERVIS YONETIMI" -ForegroundColor Yellow
        Write-Host "  -----------------------------------------------" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "    Baslat    : net start PetekServer" -ForegroundColor Gray
        Write-Host "    Durdur    : net stop PetekServer" -ForegroundColor Gray
        Write-Host "    Durum     : sc query PetekServer" -ForegroundColor Gray
        Write-Host "    Loglar    : $($script:InstallPath)\logs\" -ForegroundColor Gray
        Write-Host ""
    }

    # Bilgileri dosyaya kaydet
    $infoPath = Join-Path $script:InstallPath "CONNECTION_INFO.txt"
    $infoContent = @"
=============================================
PETEK MESSENGER SUNUCU BAGLANTI BILGILERI
=============================================
Olusturulma: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

Sunucu Adi       : $($script:ServerName)
Kurulum Dizini   : $($script:InstallPath)
Veritabani       : $($script:DbType)

ISTEMCI BAGLANTI ADRESLERI:
$(foreach ($ip in $serverIPs) {
    "  http://${ip}:$($script:Port)"
    if ($script:UseSSL) { "  https://${ip}:$($script:SSLPort)" }
})
  http://$($script:ServerName):$($script:Port)

API          : $baseUrl/api
SignalR Hub  : $baseUrl/hubs/message
Admin Panel  : $baseUrl/admin
Saglik       : $baseUrl/health

Yonetici     : $($script:AdminUser)
"@
    Set-Content -Path $infoPath -Value $infoContent -Encoding UTF8
    Write-Info "Baglanti bilgileri kaydedildi: $infoPath"
    Write-Host ""
    Write-Host "  =============================================" -ForegroundColor Green
    Write-Host ""
}

# ============================================================
# ANA AKIS
# ============================================================

try {
    Step-Welcome
    Step-InstallPath
    Step-ServerConfig
    Step-Database
    Step-AdminAccount
    Step-AdditionalConfig

    # Ozet
    Write-Host ""
    Write-Host "  =============================================" -ForegroundColor Cyan
    Write-Host "   KURULUM OZETI" -ForegroundColor Cyan
    Write-Host "  =============================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "    Dizin          : $($script:InstallPath)" -ForegroundColor White
    Write-Host "    Port           : $($script:Port)" -ForegroundColor White
    Write-Host "    SSL            : $(if ($script:UseSSL) {'Evet'} else {'Hayir'})" -ForegroundColor White
    Write-Host "    Veritabani     : $($script:DbType)" -ForegroundColor White
    Write-Host "    Yonetici       : $($script:AdminUser)" -ForegroundColor White
    Write-Host "    Windows Auth   : $(if ($script:UseWindowsAuth) {'Etkin'} else {'Devre Disi'})" -ForegroundColor White
    Write-Host "    Firewall       : $(if ($script:OpenFirewall) {'Evet'} else {'Hayir'})" -ForegroundColor White
    Write-Host "    Windows Servis : $(if ($script:InstallAsService) {'Evet'} else {'Hayir'})" -ForegroundColor White
    Write-Host ""

    if (-not (Read-YesNo "Bu ayarlarla kurulumu baslat?")) {
        Write-Host "  Kurulum iptal edildi." -ForegroundColor Yellow
        exit 0
    }

    Step-Install
    Show-ConnectionInfo
}
catch {
    Write-Host ""
    Write-Err "Beklenmeyen hata: $($_.Exception.Message)"
    Write-Host "  $($_.ScriptStackTrace)" -ForegroundColor DarkGray
    Write-Host ""
    exit 1
}
