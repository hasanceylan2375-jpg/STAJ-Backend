using System.ComponentModel.DataAnnotations;

namespace STAJ.Entities
{
    public class Konut
    {
        public int Id { get; set; }

        [Required]
        public string Baslik { get; set; } = string.Empty;

        public string? Konum { get; set; }
        public decimal Fiyat { get; set; }
        public int OdaSayisi { get; set; }
        public string? GorselUrl { get; set; }
    }
}
