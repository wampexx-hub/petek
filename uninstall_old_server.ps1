# Eski Petek Server'ı kaldırma scripti

Write-Host "=== Petek Server Kaldırılıyor ===" -ForegroundColor Yellow
Write-Host ""

# Servisi durdur
try {
    $service = Get-Service -Name "PetekServer" -ErrorAction SilentlyContinue
    if ($service) {
        Write-Host "✓ Servis durdurÃ¼luyor..." -ForegroundColor Cyan
        Stop-Service -Name "PetekServer" -Force -ErrorAction SilentlyContinue
        sc.exe delete PetekServer
        Write-Host "✓ Servis kaldırıldı" -ForegroundColor Green
    }
} catch {
    Write-Host "Servis bulunamadı (normal)" -ForegroundColor DarkGray
}

# Process'i durdur
try {
    $process = Get-Process -Name "Petek.Server" -ErrorAction SilentlyContinue
    if ($process) {
        Write-Host "✓ Sunucu process'i durduruluyor..." -ForegroundColor Cyan
        Stop-Process -Name "Petek.Server" -Force -ErrorAction SilentlyContinue
        Write-Host "✓ Process durduruldu" -ForegroundColor Green
    }
} catch {
    Write-Host "Process bulunamadı (normal)" -ForegroundColor DarkGray
}

# Firewall kuralını kaldır
try {
    Write-Host "✓ Firewall kuralı kaldırılıyor..." -ForegroundColor Cyan
    netsh advfirewall firewall delete rule name="Petek Server" | Out-Null
    Write-Host "✓ Firewall kuralı kaldırıldı" -ForegroundColor Green
} catch {
    Write-Host "Firewall kuralı bulunamadı (normal)" -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "=== Hazır! ===" -ForegroundColor Green
Write-Host "Şimdi uninstaller'ı çalıştırabilirsiniz:" -ForegroundColor Yellow
Write-Host "  'C:\Program Files\Petek Server\unins000.exe'" -ForegroundColor White
Write-Host ""
Write-Host "VEYA doğrudan yeni installer'ı çalıştırın" -ForegroundColor Yellow
