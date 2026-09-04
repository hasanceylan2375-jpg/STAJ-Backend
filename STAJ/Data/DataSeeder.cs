using Microsoft.EntityFrameworkCore;
using STAJ.Entities;

namespace STAJ.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            await context.Database.MigrateAsync();

            if (!await context.Musteriler.AnyAsync())
                await context.Musteriler.AddRangeAsync(new List<Musteri>
                {
                    new() { Ad = "Ahmet", Soyad = "Yılmaz", Telefon = "05321234567", Email = "ahmet.yilmaz@example.com" },
                    new() { Ad = "Ayşe", Soyad = "Demir", Telefon = "05329876543", Email = "ayse.demir@example.com" }
                });

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
    }
}
