using System.ComponentModel.DataAnnotations;

namespace STAJ.Entities
{
    public class Arac
    {
        public int Id { get; set; }

        [Required]
        public string Marka { get; set; } = string.Empty;

        [Required]
        public string Model { get; set; } = string.Empty;

        public int Yil { get; set; }
        public decimal Fiyat { get; set; }
        public string? GorselUrl { get; set; }
    }
}
