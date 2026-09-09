# RAG Kurulumu

Bu backend, şirket kurallarını PDF/DOCX/TXT olarak yükleyip PostgreSQL + pgvector üzerinde saklar; soru için embedding üretir, Top-K benzer chunk'ları getirir ve LLM'e yalnızca bu kaynaklarla cevap ürettirir.

## 1. PostgreSQL pgvector

Veritabanı kullanıcısının `vector` extension oluşturma yetkisi olmalıdır. Uygulama RAG ilk kez kullanıldığında `CREATE EXTENSION IF NOT EXISTS vector` çalıştırır.

## 2. OpenAI API anahtarı

API anahtarını `appsettings.json` içine yazmayın. Development ortamında User Secrets kullanın:

```powershell
dotnet user-secrets set "Rag:OpenAiApiKey" "OPENAI_API_KEYINIZ"
```

Embedding modeli varsayılan olarak `text-embedding-3-small` ve vektör boyutu 1536'dır. Sohbet modeli varsayılan olarak `gpt-4.1-mini`'dır.

## 3. API akışı

### Doküman yükleme

`POST /api/rag/documents/upload`

`multipart/form-data`:
- `companyName`: şirket adı
- `file`: PDF, DOCX veya TXT (maksimum 10 MB)

Yükleme sırasında metin çıkarılır, yaklaşık 750 token'lık chunk'lara 120 token overlap uygulanır, embedding oluşturulur ve PostgreSQL pgvector'a kaydedilir.

Doküman yükleme `AdminOnly` yetkisi ister.

### Soru sorma

`POST /api/rag/ask`

```json
{
  "companyName": "Örnek Şirket",
  "question": "Şirkette 18 yaş altı çalışma mümkün mü?"
}
```

Sistem soruyu embedding'e çevirir, şirkete göre en yakın 5 chunk'ı cosine distance ile bulur ve bu kaynakları LLM'e gönderir. Cevapla birlikte kullanılan kaynak dosya/sayfa bilgileri döner.

LLM kaynaklarda cevap bulamazsa tahmin yapmak yerine bilginin dokümanlarda bulunmadığını söylemesi için sınırlandırılmıştır.
