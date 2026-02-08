# Ürün Gereksinim Dokümanı (PRD): Petek Messenger

**Proje Adı:** Petek
**Sürüm:** 1.1.0
**Hedef Platform:** Windows (Desktop)
**Doküman Sahibi:** Ürün Yönetimi / Sistem Yönetimi

---

## 1. Ürün Vizyonu ve Amacı

"Petek", kurumsal iç iletişim için tasarlanmış, Windows ekosistemiyle tam entegre, yüksek güvenlikli ve modern bir anlık mesajlaşma uygulamasıdır. Amacı, kurumsal kimlik yönetimi (AD) ile uyumlu, denetlenebilir (Surveillance/DLP) ve kullanıcı dostu bir haberleşme altyapısı sağlamaktır.

---

## 2. Kullanıcı Rolleri

| Rol | Açıklama |
|-----|----------|
| **Kullanıcı** | Mesaj gönderen, dosya paylaşan ve durum yöneten şirket çalışanı. |
| **Moderatör** | Grup sohbetlerini yöneten, kullanıcı ekleyip çıkaran yetkili. |
| **Sistem Yöneticisi (Admin)** | AD entegrasyonunu yöneten, gözetim araçlarını bağlayan ve logları denetleyen teknik kişi. |

---

## 3. Fonksiyonel Gereksinimler

### 3.1. Kimlik ve Erişim Yönetimi

| ID | Gereksinim | Açıklama |
|----|------------|----------|
| **FR-01** | Windows AD Entegrasyonu | Uygulama, Windows Active Directory ile entegre çalışmalıdır. Kullanıcılar LDAP/Kerberos protokolleri üzerinden mevcut oturumlarıyla giriş yapabilmelidir. |
| **FR-02** | Tek Oturum Açma (SSO) | Windows'ta oturum açmış kullanıcı, uygulamayı açtığında tekrar şifre girmeden otomatik bağlanmalıdır. |
| **FR-03** | Profil Senkronizasyonu | Kullanıcının departmanı, ünvanı ve e-posta adresi doğrudan AD üzerinden çekivlmelidir. |

### 3.2. Mesajlaşma Özellikleri

| ID | Gereksinim | Açıklama |
|----|------------|----------|
| **FR-04** | Birebir ve Grup Sohbeti | Kullanıcılar kişi listesinden seçim yaparak özel veya çoklu katılımcılı gruplar oluşturabilmelidir. |
| **FR-05** | Durum (Presence) Yönetimi | **Manuel Seçim:** Uygun, Meşgul, Dışarıda, Çevrimdışı. **Otomatik Durum:** Bilgisayar kilitlendiğinde veya belirli süre işlem yapılmadığında durum "Dışarıda" olarak güncellenmelidir. |
| **FR-06** | Arkadaş ve Grup Listesi | Kullanıcılar sık görüştüğü kişileri "Favoriler" veya özel oluşturulmuş klasörler altında gruplayabilmelidir. |

### 3.3. Medya ve Dosya Paylaşımı

| ID | Gereksinim | Açıklama |
|----|------------|----------|
| **FR-07** | Dosya Transferi | Her türlü dosya formatı (PDF, DOCX, ZIP vb.) sürükle-bırak yöntemiyle gönderilebilmelidir. |
| **FR-08** | Ekran Görüntüsü Aracı | Uygulama içerisinde dahili bir ekran yakalama aracı olmalıdır. Kullanıcı ekranın belirli bir alanını seçip, üzerine basit çizimler yapıp doğrudan sohbete gönderebilmelidir. |

### 3.4. Entegrasyon ve Güvenlik (Sistem Yönetimi)

| ID | Gereksinim | Açıklama |
|----|------------|----------|
| **FR-09** | Surveillance (Gözetim) Entegrasyonu | Tüm yazışmalar ve paylaşılan dosyalar, kurumsal gözetim araçları (DLP, Arşivleme servisleri) tarafından taranabilmesi için API/Webhook desteği sunmalıdır. |
| **FR-10** | Uçtan Uca Şifreleme (Opsiyonel) | Veritabanında mesajlar şifreli saklanmalı, yalnızca yetkili adminler veya denetim araçları decryption yetkisine sahip olmalıdır. |

---

## 4. Teknik Gereksinimler ve Mimari Önerisi

### 4.1. Teknoloji Stack'i

| Katman | Teknoloji | Açıklama |
|--------|-----------|----------|
| **Frontend (UI/UX)** | WPF (.NET 8/9) | Modern ve şık görünüm. Fluent Design sistemine (Windows 11 stili) sadık kalınmalıdır. |
| **Backend** | SignalR veya WebSockets | Yüksek performanslı mesaj iletimi için gerçek zamanlı iletişim altyapısı. |
| **Veritabanı (İlişkisel)** | PostgreSQL | Kullanıcı ve grup ilişkileri için. |
| **Veritabanı (Cache)** | Redis | Anlık mesaj cache mekanizması için. |
| **API** | RESTful | Surveillance araçları için dışarıya açık güvenli endpointler. |

### 4.2. Mimari Diyagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         Windows Desktop                          │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              Petek Messenger (WPF)                      │   │
│  │  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐   │   │
│  │  │  Chat   │  │ Contacts│  │  Files  │  │ Settings│   │   │
│  │  │  View   │  │  View   │  │  View   │  │  View   │   │   │
│  │  └────┬────┘  └────┬────┘  └────┬────┘  └────┬────┘   │   │
│  │       └────────────┴────────────┴────────────┘         │   │
│  │                        │                               │   │
│  │              ┌─────────┴─────────┐                     │   │
│  │              │   SignalR Client  │                     │   │
│  │              └─────────┬─────────┘                     │   │
│  └────────────────────────┼─────────────────────────────────┘   │
└──────────────────────────┼──────────────────────────────────────┘
                           │
                           │ WebSocket/HTTPS
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Backend Server                            │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                    ASP.NET Core API                      │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │   │
│  │  │ Auth/SSO    │  │  SignalR    │  │  REST API   │     │   │
│  │  │ (AD/LDAP)   │  │    Hub      │  │ (Webhook)   │     │   │
│  │  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘     │   │
│  │         └────────────────┼────────────────┘            │   │
│  └──────────────────────────┼────────────────────────────────┘   │
│                             │                                    │
│    ┌────────────────────────┼────────────────────────┐          │
│    │                        ▼                        │          │
│    │  ┌──────────────┐  ┌──────────────┐           │          │
│    │  │  PostgreSQL  │  │    Redis     │           │          │
│    │  │  (Users/     │  │   (Cache/    │           │          │
│    │  │   Groups)    │  │   Sessions)  │           │          │
│    │  └──────────────┘  └──────────────┘           │          │
│    └─────────────────────────────────────────────────┘          │
└─────────────────────────────────────────────────────────────────┘
                           │
                           │ API/Webhook
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                   External Integrations                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐             │
│  │ Active      │  │ DLP/        │  │ Archiving   │             │
│  │ Directory   │  │ Surveillance│  │ Service     │             │
│  └─────────────┘  └─────────────┘  └─────────────┘             │
└─────────────────────────────────────────────────────────────────┘
```

---

## 5. Tasarım ve Arayüz (UI) Beklentileri

### 5.1. Genel Tasarım Prensipleri

| Özellik | Açıklama |
|---------|----------|
| **Karanlık Mod Desteği** | Sistem temasına göre otomatik değişen Dark/Light mode. |
| **Akrilik Efektleri** | Arka planda Windows "Mica" veya "Acrylic" efektli transparan geçişler. |
| **Sidebar Navigasyonu** | Sol tarafta daraltılabilir, ikon odaklı ana menü. |
| **Okundu Bilgisi** | Mesajların iletildi ve okundu durumlarını gösteren görsel işaretler. |

### 5.2. Arayüz Wireframe

```
┌────────────────────────────────────────────────────────────────────┐
│ ┌──┐  Petek Messenger                              ─ □ ×           │
│ └──┘                                                               │
├────────┬───────────────────────┬───────────────────────────────────┤
│        │ 🔍 Ara...             │  Ahmet Yılmaz                     │
│  💬   ├───────────────────────┤  IT Departmanı                    │
│        │ ★ Favoriler          │                                    │
│  👥   │   ├─ Ali Veli    🟢   │  ┌────────────────────────────┐   │
│        │   └─ Ayşe Kaya  🟡   │  │ Merhaba, toplantı saat     │   │
│  📁   ├───────────────────────┤  │ kaçta başlıyor?            │   │
│        │ 📁 Departmanlar      │  │                    14:32 ✓✓│   │
│  ⚙️   │   ├─ IT              │  └────────────────────────────┘   │
│        │   ├─ İK             │                                    │
│        │   └─ Muhasebe       │  ┌────────────────────────────┐   │
│        ├───────────────────────┤  │ Saat 15:00'te konferans    │   │
│        │ Son Sohbetler        │  │ odasında olacağız.         │   │
│        │   ├─ Proje Grubu     │  │ 14:35                      │   │
│        │   └─ Destek Ekibi    │  └────────────────────────────┘   │
│        │                       │                                    │
│        │                       │  ┌────────────────────────────┐   │
│        │                       │  │ Tamam, orada olacağım.     │   │
│        │                       │  │                    14:36 ✓✓│   │
│        │                       │  └────────────────────────────┘   │
│        │                       ├───────────────────────────────────┤
│        │                       │ 📎  📷  😊  │ Mesaj yazın...  │➤│
└────────┴───────────────────────┴───────────────────────────────────┘

Legend:
🟢 Uygun (Available)
🟡 Dışarıda (Away)
🔴 Meşgul (Busy)
⚫ Çevrimdışı (Offline)
✓  İletildi (Delivered)
✓✓ Okundu (Read)
```

---

## 6. Admin Paneli Özellikleri

### 6.1. Yönetim Fonksiyonları

| Özellik | Açıklama |
|---------|----------|
| Kullanıcı Yönetimi | Kullanıcı yetkilendirme ve rollerin yönetimi. |
| Dosya Politikaları | Dosya paylaşım limitlerinin (boyut/tür) belirlenmesi. |
| Oturum Yönetimi | Aktif oturumların takibi ve gerektiğinde sonlandırılması. |
| Entegrasyon Yönetimi | Surveillance araçları için bağlantı (API Key/Webhook) yönetimi. |

### 6.2. Admin Panel Wireframe

```
┌────────────────────────────────────────────────────────────────────┐
│  Petek Admin Panel                                   Admin ▼      │
│  ├────────┬───────────────────────────────────────────────────────────┤
│        │ Dashboard                                                 │
│  📊   │ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐          │
│        │ │ Aktif       │ │ Günlük      │ │ Dosya       │          │
│  👥   │ │ Kullanıcı   │ │ Mesaj       │ │ Transferi   │          │
│        │ │    247     │ │   12.4K    │ │   1.2 GB   │          │
│  📁   │ └─────────────┘ └─────────────┘ └─────────────┘          │
│        │                                                           │
│  🔗   │ Kullanıcı Yönetimi                                       │
│        │ ┌──────────────────────────────────────────────────────┐ │
│  📋   │ │ Ad           │ Departman │ Rol        │ Durum │ İşlem││
│        │ ├──────────────────────────────────────────────────────┤ │
│  ⚙️   │ │ Ali Veli     │ IT        │ Kullanıcı │ 🟢   │ ✏️🗑️││
│        │ │ Ayşe Kaya    │ İK        │ Moderatör │ 🟢   │ ✏️🗑️││
│        │ │ Mehmet Öz    │ Muhasebe  │ Kullanıcı │ ⚫   │ ✏️🗑️││
│        │ └──────────────────────────────────────────────────────┘ │
│        │                                                           │
│        │ Surveillance Entegrasyonu                                │
│        │ ┌──────────────────────────────────────────────────────┐ │
│        │ │ DLP Webhook: https://dlp.company.com/api/v1/ingest  ││
│        │ │ API Key: ●●●●●●●●●●●●●●●●  [Yenile] [Test Et]       ││
│        │ │ Durum: ✅ Bağlı                                      ││
│        │ └──────────────────────────────────────────────────────┘ │
└────────┴───────────────────────────────────────────────────────────┘
```

---

## 7. Kabul Kriterleri

| ID | Kriter | Ölçüt |
|----|--------|-------|
| **AC-01** | SSO Giriş Süresi | Kullanıcı Windows kimlik bilgileriyle **3 saniyenin altında** sisteme giriş yapabilmeli. |
| **AC-02** | Ekran Görüntüsü İş Akışı | Ekran görüntüsü alma ve gönderme işlemi **en fazla 3 tıklama** ile tamamlanmalı. |
| **AC-03** | Mesaj İletim Gecikmesi | Gönderilen bir mesaj, alıcıya **200ms gecikmenin altında** ulaşmalı. |
| **AC-04** | Surveillance Entegrasyonu | Admin paneli üzerinden gözetim araçlarına veri akışı doğrulanabilmeli. |

---

## 8. Proje Zaman Çizelgesi (Önerilen)

| Faz | Süre | Çıktılar |
|-----|------|----------|
| **Faz 1: Temel Altyapı** | 4 Hafta | AD entegrasyonu, SSO, temel mesajlaşma |
| **Faz 2: UI/UX Geliştirme** | 3 Hafta | Fluent Design UI, karanlık mod, sidebar |
| **Faz 3: Gelişmiş Özellikler** | 3 Hafta | Dosya transferi, ekran görüntüsü aracı |
| **Faz 4: Admin & Entegrasyon** | 2 Hafta | Admin paneli, Surveillance API |
| **Faz 5: Test & Optimizasyon** | 2 Hafta | Performans testleri, güvenlik denetimi |

---

## 9. Riskler ve Azaltma Stratejileri

| Risk | Olasılık | Etki | Azaltma Stratejisi |
|------|----------|------|---------------------|
| AD entegrasyon sorunları | Orta | Yüksek | Erken prototip ile doğrulama, AD test ortamı kurulumu |
| Performans hedeflerine ulaşamama | Düşük | Yüksek | SignalR optimizasyonları, Redis cache stratejisi |
| Surveillance API uyumsuzluğu | Orta | Orta | Esnek webhook yapısı, API versiyonlama |
| Windows 11 uyumluluk sorunları | Düşük | Orta | Windows App SDK kullanımı, erken test |

---

## 10. Başarı Metrikleri

| Metrik | Hedef |
|--------|-------|
| Günlük Aktif Kullanıcı (DAU) | Pilot grupta %80 aktif kullanım |
| Mesaj İletim Başarı Oranı | %99.9 |
| Ortalama Mesaj Gecikmesi | <100ms |
| Kullanıcı Memnuniyet Skoru | 4.5/5 |
| Sistem Uptime | %99.95 |

---

## 11. Onay

| Rol | İsim | Tarih | İmza |
|-----|------|-------|------|
| Ürün Yöneticisi | | | |
| Teknik Lider | | | |
| Sistem Yöneticisi | | | |
| Güvenlik Sorumlusu | | | |

---

*Bu doküman, Petek Messenger projesinin 1.1.0 sürümü için temel gereksinimleri ve beklentileri tanımlamaktadır. Değişiklikler, ilgili paydaşların onayı ile güncellenecektir.*
