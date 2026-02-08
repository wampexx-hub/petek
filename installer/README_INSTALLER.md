# Petek Messenger Kurulum Sihirbazı Oluşturma

Bu dizindeki `Setup.iss` dosyası, uygulamanız için profesyonel bir kurulum sihirbazı (setup.exe) oluşturmanızı sağlar.

## Gereksinimler

1.  **Inno Setup**: [jrsoftware.org](https://jrsoftware.org/isdl.php) adresinden Inno Setup programını indirin ve kurun.
2.  **Derlenmiş Dosyalar**: Uygulamanın en son sürümünün `publish/desktop` klasöründe olduğundan emin olun.

## Oluşturma Adımları

1.  Terminalde projenin kök dizinine gidin.
2.  Aşağıdaki komutu çalıştırarak uygulamayı yayınlayın:
    ```powershell
    dotnet publish src/Petek.Desktop/Petek.Desktop.csproj -c Release -r win-x64 --self-contained false -o publish/desktop
    ```
3.  `installer/Setup.iss` dosyasına sağ tıklayıp "Compile" (Derle) seçeneğini seçin veya Inno Setup Compiler ile açıp derleyin.
4.  Oluşturulan kurulum dosyası `publish/installer/PetekSetup.exe` konumunda olacaktır.

## Özellikler

*   Türkçe ve İngilizce dil desteği.
*   Masaüstü kısayolu oluşturma seçeneği.
*   Windows Program Ekle/Kaldır entegrasyonu.
*   Modern arayüz (WPF uygulamasına uygun).
