using Microsoft.EntityFrameworkCore;
using STAJ.Entities;

namespace STAJ.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            await context.Database.MigrateAsync();

            await SeedMusterilerAsync(context);

            if (!await context.Sirketler.AnyAsync())
                await context.Sirketler.AddRangeAsync(new List<Sirket>
                {
                    new() { Ad = "Vizyon Teknoloji", Sektor = "Yazılım", Email = "info@vizyon.com", Telefon = "02120000001" },
                    new() { Ad = "Grand Yapı", Sektor = "İnşaat", Email = "info@grandyapi.com", Telefon = "02120000002" }
                });

            if (!await context.Konutlar.AnyAsync())
                await context.Konutlar.AddRangeAsync(new List<Konut>
                {
                    new() { Baslik = "Modern 3+1 Daire", Konum = "İstanbul", OdaSayisi = 3, Fiyat = 4250000 },
                    new() { Baslik = "Bahçeli Villa", Konum = "Ankara", OdaSayisi = 5, Fiyat = 8500000 }
                });

            if (!await context.Araclar.AnyAsync())
                await context.Araclar.AddRangeAsync(new List<Arac>
                {
                    new() { Marka = "Toyota", Model = "Corolla", Yil = 2023, Fiyat = 1200000 },
                    new() { Marka = "Renault", Model = "Clio", Yil = 2022, Fiyat = 850000 }
                });

            await context.SaveChangesAsync();
        }

        private static async Task SeedMusterilerAsync(AppDbContext context)
        {
            const int hedefSeedMusteri = 1000;
            const string seedEmailDomain = "@seed.staj.local";

            var mevcutSeedSayisi = await context.Musteriler
                .CountAsync(x => x.Email.EndsWith(seedEmailDomain));

            if (mevcutSeedSayisi >= hedefSeedMusteri)
                return;

            var adlar = new[]
            {
                "Ahmet", "Mehmet", "Mustafa", "Ali", "Hasan", "Hüseyin", "Emre", "Burak", "Mert", "Can",
                "Ayşe", "Fatma", "Zeynep", "Elif", "Esra", "Buse", "Ece", "Ceren", "Seda", "Derya"
            };

            var soyadlar = new[]
            {
                "Yılmaz", "Kaya", "Demir", "Çelik", "Şahin", "Yıldız", "Yıldırım", "Aydın", "Öztürk", "Arslan",
                "Doğan", "Kılıç", "Aslan", "Koç", "Kurt", "Özdemir", "Erdoğan", "Aksoy", "Güneş", "Polat"
            };

            var musteriler = new List<Musteri>(hedefSeedMusteri - mevcutSeedSayisi);

            for (var i = mevcutSeedSayisi + 1; i <= hedefSeedMusteri; i++)
            {
                var ad = adlar[(i - 1) % adlar.Length];
                var soyad = soyadlar[((i - 1) / adlar.Length) % soyadlar.Length];

                musteriler.Add(new Musteri
                {
                    Ad = ad,
                    Soyad = soyad,
                    Telefon = $"05{(300000000 + i):000000000}",
                    Email = $"musteri{i:0000}{seedEmailDomain}",
                    TcKimlikNo = (10000000000L + i).ToString(),
                    DogumTarihi = new DateTime(1970 + (i % 30), (i % 12) + 1, (i % 27) + 1)
                });
            }

            await context.Musteriler.AddRangeAsync(musteriler);
        }
    }
}
