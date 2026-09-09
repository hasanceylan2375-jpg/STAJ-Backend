using System.ComponentModel.DataAnnotations;

namespace STAJ.Entities
{
    public class Arac
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Marka { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Model { get; set; } = string.Empty;

        public int Yil { get; set; }
        public decimal Fiyat { get; set; }

        [StringLength(2048)]
        public string? GorselUrl { get; set; }
    }
}
