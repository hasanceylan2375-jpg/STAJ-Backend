# Müşteri Ekleme Workflow + Onay

STAJ 43 akışı: Kullanıcı müşteri formunu doldurur ve gerçek müşteri kaydı yerine `Pending` workflow talebi oluşturur. Müdür rolü olarak mevcut sistemdeki `Admin` rolü kullanılır.

## Durum akışı

`Draft -> Pending -> Manager Approval -> Approved / Rejected`

- `Pending`: kullanıcı talebi oluşturdu.
- `Approved`: müdür onayladı; onay sırasında müşteri kaydı veritabanına eklenir.
- `Rejected`: müdür talebi reddetti; müşteri kaydı oluşturulmaz.

## API

- `POST /api/workflows/start` — kullanıcı talebi başlatır.
- `GET /api/workflows` — kullanıcının kendi taleplerini görür.
- `GET /api/workflows/pending` — Admin/müdür bekleyen talepleri görür.
- `POST /api/workflows/{id}/approve` — Admin/müdür onaylar ve müşteriyi oluşturur.
- `POST /api/workflows/{id}/reject` — Admin/müdür reddeder.

Workflow verisi `WorkflowRequests` tablosunda JSON olarak saklanır. Uygulama açılışında tablo/index'leri yoksa oluşturulur; mevcut müşteri kayıtları korunur.
