using Microsoft.EntityFrameworkCore;
using STAJ.Entities;

namespace STAJ.Data
{
    public class AppDbContext : DbContext
    {
        
            public AppDbContext(DbContextOptions<AppDbContext> options)
                : base(options)
            {
            }

            public DbSet<Musteri> Musteriler { get; set; }
        
    }
}
