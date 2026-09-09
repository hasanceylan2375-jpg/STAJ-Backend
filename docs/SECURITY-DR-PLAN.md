# Backend Güvenlik – Yedekleme ve DR Planı

## Amaç

Uygulama ve PostgreSQL verilerinin kaybı, bozulması veya servis kesintisi durumunda kontrollü geri dönüş sağlamak.

## Yedekleme politikası

- PostgreSQL için günlük tam yedek alınır.
- Kritik üretim ortamlarında mümkünse günlük yedeğe ek olarak WAL/point-in-time recovery uygulanır.
- Yedekler uygulama sunucusundan ayrı ve erişim kontrollü bir depolamada tutulur.
- Yedekleme dosyaları şifrelenir ve üretim kimlik bilgileri kaynak koduna yazılmaz.
- En az 30 günlük geri dönüş noktası korunur.

## Geri yükleme prosedürü

1. Olayı tespit et ve etkilenen servisi izole et.
2. En son sağlıklı yedekleme noktasını belirle.
3. PostgreSQL veritabanını güvenli bir ortama geri yükle.
4. Uygulama migration durumunu kontrol et.
5. Backend'i yeniden başlat ve sağlık kontrollerini doğrula.
6. Kritik API işlemlerini test et.
7. Servisi kontrollü şekilde tekrar trafiğe aç.
8. Olay sonrası kayıtları ve kök nedeni dokümante et.

## RPO / RTO hedefleri

- RPO: 24 saat veya daha iyi.
- RTO: 4 saat veya daha iyi.
- Üretim ortamının ihtiyaçlarına göre bu hedefler daha sıkı değerlere çekilebilir.

## Sorumluluk ve test

- Yedekleme işlemleri otomatikleştirilmeli ve başarısız yedekler için alarm üretilmelidir.
- Geri yükleme prosedürü en az üç ayda bir test edilmelidir.
- Test sonuçları, geri yükleme süresi ve tespit edilen eksikler kayıt altına alınmalıdır.

> Not: Bu belge uygulama deposunda DR prosedürünü tanımlar. Gerçek üretim yedekleri, bağlantı bilgileri ve gizli anahtarlar Git deposunda tutulmamalıdır.
