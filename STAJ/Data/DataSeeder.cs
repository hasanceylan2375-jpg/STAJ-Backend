using Microsoft.EntityFrameworkCore;
using STAJ.Entities;

namespace STAJ.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            await context.Database.MigrateAsync();

            if (await context.Musteriler.AnyAsync())
                return;

            var musteriler = new List<Musteri>
            {
                new() { Ad = "Ahmet", Soyad = "Yılmaz", Telefon = "05321234567", Email = "ahmet.yilmaz@example.com" },
                new() { Ad = "Ayşe", Soyad = "Demir", Telefon = "05329876543", Email = "ayse.demir@example.com" },
                new() { Ad = "Mehmet", Soyad = "Kaya", Telefon = "05324567890", Email = "mehmet.kaya@example.com" },
                new() { Ad = "Zeynep", Soyad = "Şahin", Telefon = "05327654321", Email = "zeynep.sahin@example.com" },
                new() { Ad = "Can", Soyad = "Aydın", Telefon = "05321112233", Email = "can.aydin@example.com" }
            };

            await context.Musteriler.AddRangeAsync(musteriler);
            await context.SaveChangesAsync();
        }
    }
}
