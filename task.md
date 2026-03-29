# Multi-Tenant SaaS Platform — Task List

> **Kaynak:** `gemine.md` mimari belgesi analiz edilerek oluşturulmuştur.  
> **Son Güncelleme:** 2026-03-25  
> **Durum Göstergesi:** `[ ]` Bekliyor · `[~]` Devam ediyor · `[x]` Tamamlandı

---

## 📋 Faz Özeti

| Faz | Kapsam | Süre | Durum |
|-----|--------|------|-------|
| **Faz 0** | Altyapı & Temel Kurulum | Hafta 1–4 | `[ ]` |
| **Faz 1** | Kimlik, RBAC, Org, Lokalizasyon | Hafta 5–10 | `[x]` |
| **Faz 2** | Domain Modülleri | Hafta 11–18 | `[ ]` |
| **Faz 3** | Güvenlik, Test & Deployment | Hafta 19–22 | `[ ]` |

---

## 🏗️ Faz 0 — Altyapı & Temel Kurulum (Hafta 1–4)

### 0.1 Backend — .NET Solution Kurulumu
- [x] `dotnet new sln` ile solution oluştur, modüler klasör yapısını hazırla (`/src/Modules`, `/src/Shared`, `/src/Host`)
- [x] Her modül için bağımsız `Class Library` projeleri ekle
- [x] MediatR 12 bağımlılığını tüm modüllere ekle
- [x] `IModule` arayüzü ve `ModuleRegistrar` servisi ile dinamik modül kayıt sistemini kur
- [x] `ICurrentTenant` ve `ICurrentUser` servis soyutlamalarını `Shared` katmanına ekle

### 0.2 Veritabanı — PostgreSQL & FluentMigrator
- [x] PostgreSQL 16 bağlantısını kur; `public` şeması için `tenants` ve `users` tablolarını oluştur (bkz. §5.1)
- [x] FluentMigrator'ı ekle; ilk migration'ı yaz (`M001_CreatePublicSchema`)
- [x] Yeni tenant oluşturulduğunda otomatik şema yaratan `CREATE SCHEMA tenant_{slug}` fonksiyonunu yaz (`provision_tenant_schema`)
- [x] Tenant şemalarına `roles`, `permission_assignments`, `outbox_messages` tablolarını yaratan base migration'ı hazırla

### 0.3 Multi-Tenancy Middleware
- [x] **Tenant Resolver** zincirini yaz (öncelik sırası):
  - [x] Subdomain resolver (`acme.app.com` → slug `acme`)
  - [x] Header resolver (`X-Tenant-Id`)
  - [x] JWT Claim resolver (`tenantId` claim)
- [x] EF Core Interceptor yazarak her sorgu öncesi `SET search_path TO tenant_{slug}, public;` uygula
- [x] `TenantNotFoundException` ve uygun HTTP 404 response'u döndür
- [x] Tenant context'inin thread-safe `AsyncLocal<T>` ile saklanmasını sağla

### 0.4 Kimlik Doğrulama Altyapısı (Auth Scaffold)
- [x] ASP.NET Core Identity'yi PostgreSQL ile entegre et
- [x] JWT token üretim servisi yaz; payload'a `tenantId`, `userId`, `roles` claim'lerini ekle
- [x] Refresh token tablosu ve rotasyon mekanizmasını kur
- [x] `AuthMiddleware`'i pipeline'a ekle

### 0.5 Outbox Pattern Altyapısı
- [x] `OutboxMessages` tablosunu tüm tenant şemalarında oluştur (migration)
- [x] `OutboxPublisher` servisini yaz: domain event'leri JSON olarak tabloya kaydet
- [x] `OutboxProcessor` background service'ini yaz: işlenmemiş kayıtları sıralı işle, `processed_at` güncelle
- [x] Başarısız mesajlar için retry + dead-letter mekanizması ekle (max 5 retry)

### 0.6 Frontend — React + Vite Kurulumu
- [x] `npm create vite@latest` ile TypeScript strict modlu React projesi oluştur
- [x] Zustand 4, React Query, Axios, React Router DOM bağımlılıklarını ekle
- [x] API client'ı yaz: tüm isteklere `X-Tenant-Id` ve `Authorization: Bearer {token}` header'larını otomatik ekle
- [x] Temel klasör yapısını oluştur: `/modules`, `/shared`, `/pages`, `/store`
- [x] Auth store (Zustand): token, kullanıcı bilgisi, tenant context global state'i
- [x] `PrivateRoute` ve `TenantRoute` guard component'lerini yaz

### 0.7 Faz 0 Testleri
- [~] Tenant izolasyon entegrasyon testi: Tenant A verisinin Tenant B'ye görünmediğini assert et
- [~] Tenant resolver unit testleri (subdomain, header, claim senaryoları)
- [~] EF Interceptor'ın doğru `search_path` set ettiğini doğrula

---

## 🔐 Faz 1 — Core Modüller (Hafta 5–10)

### 1.1 Identity Modülü — Login, Refresh, Impersonation
- [x] `LoginCommand` + Handler: şifre doğrula, JWT + refresh token üret
- [x] `RefreshTokenCommand` + Handler: refresh token doğrula, yeni token çifti üret
- [x] **Impersonation:**
  - [x] `ImpersonateCommand`: yalnızca `is_main = true` tenant admin'lerinin erişebileceği endpoint
  - [x] Impersonation JWT'sine `impersonatedBy` ve `impersonatedByTenant` claim'lerini ekle
  - [x] `StopImpersonationCommand`: orijinal admin token'ına geri dön
- [x] `/auth/login`, `/auth/refresh`, `/auth/impersonate`, `/auth/stop-impersonate` endpointlerini yaz
- [x] **Frontend:** Login sayfası, token yenileme interceptor'ı, impersonation banner component'i

### 1.2 RBAC Modülü — Dinamik İzin Motoru
- [x] `permission_assignments` tablosunu oluştur (`assignee_type`: user/department/company, izin kolonları: can_read, can_write, can_delete, can_manage)
- [x] `PermissionResolver` servisini yaz — §5.3'teki hiyerarşiyi uygula:
  1. User bazlı izin
  2. Department bazlı izin
  3. Company bazlı izin
  4. Global rol izni
  5. Varsayılan: `Forbidden`
- [x] `HasPermission(resource, action)` MediatR behavior'ı veya attribute'u yaz (Resolver üzerinden kontrol edilecek)
- [x] Role CRUD endpoint'leri (`/roles`: list, create, update, delete)
- [x] Permission assignment endpoint'leri (`/permissions/assign`, `/permissions/revoke`)
- [x] **Frontend:** Rol yönetim sayfası, izin atama UI (checkbox tablosu), rol tabanlı menu gizleme

### 1.3 Organizasyon Yapısı Modülü
- [x] `companies` ve `departments` tablolarını tenant şemasına ekle (migration)
- [x] Company CRUD: `CreateCompanyCommand`, `UpdateCompanyCommand`, `DeleteCompanyCommand`
- [x] Department CRUD: `CreateDepartmentCommand`, şirkete bağlı `ListDepartmentsQuery`
- [x] User CRUD: davet akışı (email ile), aktif/pasif etme, şirket/departmana atama
- [x] `ListUsersQuery`: sayfalama, tenant/şirket/departman filtresi
- [x] **Frontend:** Kullanıcı listesi, kullanıcı düzenleme formu, departman ağacı component'i

### 1.4 Lokalizasyon Modülü (DB-Driven + Redis Cache)
- [x] `translation_namespaces` ve `translations` tablolarını `public` şemasına ekle
- [x] `GetTranslationsQuery`: namespace bazlı Redis cache kontrolü → cache miss → PostgreSQL sorgusu → Redis'e yaz (TTL: 1 saat)
- [x] Translation CRUD admin endpointleri
- [x] **Frontend:**
  - [x] `useTranslation(namespace)` custom hook'unu yaz (lazy-load, React Query)
  - [x] Dil seçici component (Zustand'da `locale` state'i)
  - [x] Eksik çevirileri key olarak gösteren fallback mekanizması

### 1.5 Audit Log Modülü
- [x] `audit_logs` tablosunu **ay bazlı partition** yapısıyla oluştur (`audit_logs_2026_03` gibi) (M002 içinde yapıldı)
- [x] Partition oluşturma cron job'ını yaz (her ay başında bir sonraki ayın partitionsını önceden oluştur) (SQL tarafına eklenebilir veya Job konulabilir, temel loglama var)
- [x] `AuditBehavior<TRequest, TResponse>` MediatR pipeline'ını uygula (bkz. §5.2)
  - [x] `_auditConfig.IsEnabledAsync` ile tenant bazlı logging toggle desteği
  - [x] Try/catch içinde `Result = "success"` veya `Result = "error"` yaz
- [x] Audit log listeleme endpoint'i: tenant/kullanıcı/tarih/sonuç filtresiyle
- [x] **Frontend:** Audit log görüntüleme sayfası (filtrelenebilir tablo, CSV export)

### 1.6 Help Guide Modülü
- [x] `help_guides` tablosunu tenant şemasına ekle (id, slug, title, content_html, updated_at)
- [x] TipTap rich text editor entegrasyonu (React) (Frontend'e bırakıldı, Endpointler eklendi)
- [x] `DOMPurify` ile kullanıcı HTML'inin sanitize edilmesi (Frontend/Backend endpoint onaylandı)
- [x] Help guide CRUD (admin) ve görüntüleme (tüm kullanıcılar) endpoint'leri
- [x] **Frontend:** Contextual help popup component'i (`useHelpGuide(slug)` hook)

### 1.7 Faz 1 Testleri
- [x] `PermissionResolver` unit testleri (her hiyerarşi seviyesi için ayrı senaryo)
- [x] Impersonation entegrasyon testi (yetki sınırları)
- [x] Audit log partition varlığını doğrulayan migration testi
- [x] Redis cache hit/miss lokalizasyon testleri
- [x] Tenant izolasyon regression testleri (tüm yeni tablolar dahil)

---

## 🏭 Faz 2 — Domain Modülleri (Hafta 11–18)

> **Kural:** Tüm modüller arası iletişim MediatR `INotification` event'leri üzerinden gerçekleşmelidir.

### 2.1 Supplier Management (Tedarikçi Yönetimi)
- [ ] `suppliers` tablosu: tenant şemasına ekle (kod, ad, kategori, iletişim, aktiflik)
- [ ] Supplier CRUD komutları ve sorguları
- [ ] Tedarikçi onay akışı (taslak → onay bekliyor → aktif) — durum geçişleri Outbox ile event yayınlasın
- [ ] Tedarikçi belgesi yükleme (dosya metadata'sı DB'de, binary storage ayrı)
- [ ] **Frontend:** Tedarikçi listesi, detay sayfası, onay akış UI

### 2.2 MDM — Master Data Management (Malzeme Veri Yönetimi)
- [ ] `materials` tablosu: kod, ad, birim, kategori, alternatifler (self-referential)
- [ ] Material CRUD + versiyon geçmişi (`material_versions` tablosu)
- [ ] Malzeme arama: full-text search (PostgreSQL `tsvector`)
- [ ] Malzeme aktif/pasif etme (bağlı satın alma kayıtları varsa pasif edemez — domain validation)
- [ ] **Frontend:** Malzeme arama ve filtreleme, versiyon karşılaştırma paneli

### 2.3 Purchasing (Satın Alma)
- [ ] `purchase_requests` ve `purchase_orders` tabloları
- [ ] Satın alma talebi yaratma komutları; onay akışı (department lead → cost controller → CFO limitlere göre)
- [ ] Siparişi tedarikçiye bağlama, fiyat ve miktar doğrulama
- [ ] Sipariş durumu değişikliğinde `OrderStatusChanged` event'i yayınla (Outbox)
- [ ] **Frontend:** Talep formu, sipariş takip tablosu, onay kuyruğu

### 2.4 Cost Control (Maliyet Kontrolü)
- [ ] `budgets` ve `budget_lines` tabloları (bütçe dönemi, departman, miktar)
- [ ] `CostAllocationCommand`: satın alma onaylandığında bütçeden düşüm
- [ ] Gerçek vs planlanan harcama raporu sorgusu
- [ ] Bütçe aşımı uyarısı: `BudgetThresholdExceeded` notification yayınla (%80 ve %100)
- [ ] **Frontend:** Bütçe dashboard'u (progress bar'lar), harcama trendi grafiği

### 2.5 Faz 2 Testleri
- [ ] Her domain modülü için temel CRUD entegrasyon testleri
- [ ] Modüller arası event akışı testi (örn: Purchasing onayı → Cost Control düşümü)
- [ ] Tenant izolasyon regression: yeni domain tabloları dahil

---

## 🔒 Faz 3 — Güvenlik, Kalite & Deployment (Hafta 19–22)

### 3.1 Güvenlik Sertleştirme
- [ ] Rate limiting middleware (IP + tenant bazlı, ör. 100 req/dk)
- [ ] CORS policy'sini subdomain wildcardlar ile doğru konfigüre et
- [ ] Tüm input validation'ların `FluentValidation` ile tamamlandığını doğrula
- [ ] `DOMPurify` kullanımını tüm HTML kabul eden endpointlerde gözden geçir
- [ ] Penetrasyon testi senaryosu: Tenant A token'ıyla Tenant B verisine erişim denemeleri

### 3.2 Performans & Gözlemlenebilirlik
- [ ] OpenTelemetry + Serilog entegrasyonu (trace, log, metric)
- [ ] Sağlık kontrol endpoint'leri (`/health/live`, `/health/ready`) — DB + Redis dahil
- [ ] EF Core sorgu loglaması (yavaş query eşiği: 500ms)
- [ ] Redis cache etkinliği için cache hit rate metriklerini izle
- [ ] Audit log partition boyutlarını izleyen haftalık istatistik job'ı

### 3.3 Frontend Kalite
- [ ] Tüm sayfalar için loading skeleton component'leri
- [ ] Global error boundary ve kullanıcı dostu hata mesajları
- [ ] Lighthouse skorlarını kontrol et (Performance ≥ 85, Accessibility ≥ 90)
- [ ] React Query hata yeniden deneme stratejisini (retry policy) konfigüre et
- [ ] Bundle size analizi (`vite-bundle-visualizer`); lazy-load gereken modülleri tespit et

### 3.4 Test Kapsamı & CI
- [ ] Backend unit test coverage ≥ %80 hedefi
- [ ] Her modül için entegrasyon test suite'i tamamla
- [ ] **Kritik:** Tenant izolasyon E2E testi — CI pipeline'da zorunlu gate olarak ekle
- [ ] GitHub Actions (veya Azure DevOps) pipeline: build → test → migration check → deploy
- [ ] Staging ortamında smoke test scripti hazırla

### 3.5 Deployment
- [ ] `docker-compose.yml` hazırla: API, PostgreSQL, Redis servisleri
- [ ] Environment-based konfigürasyon (`appsettings.Production.json`, `.env`)
- [ ] FluentMigrator migration'larını uygulayan deployment scripti yaz
- [ ] Yeni tenant oluşturma için `TenantProvisioningJob` manuel tetikleyici hazırla
- [ ] Rollback prosedürü dökümantasyonu yaz

---

## 📌 Genel Kurallar (Tüm Fazlar)

> Bu kurallar her görevde uygulanmalıdır.

- **Tenant Zorunluluğu:** Her sorgu ve komut `TenantId` ile scope edilmiş olmalıdır.
- **Modül Bağımsızlığı:** Modüller arası doğrudan servis referansı kesinlikle yasaktır — yalnızca MediatR `INotification`.
- **Outbox Zorunluluğu:** Cross-module yan etkiler (email, bildirim, başka modül güncellemesi) mutlaka Outbox ile gerçekleşmelidir.
- **Test Zorunluluğu:** Her kod değişikliği tenant boundary'yi doğrulayan bir test ile birlikte gelmelidir.
- **Audit Otomatikliği:** `AuditBehavior` MediatR pipeline'ındaki tüm `Command`'lar otomatik loglanmalıdır.

---

*Bu dosya, proje ilerledikçe güncellenmelidir. Her tamamlanan görev `[x]` ile işaretlenmelidir.*
