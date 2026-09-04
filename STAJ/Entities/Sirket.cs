using System.ComponentModel.DataAnnotations;

namespace STAJ.Entities
{
    public class Sirket
    {
        public int Id { get; set; }

        [Required]
        public string Ad { get; set; } = string.Empty;

        public string? LogoUrl { get; set; }
        public string? Sektor { get; set; }
        public string? Email { get; set; }
        public string? Telefon { get; set; }
    }
}
