using System.ComponentModel.DataAnnotations;

namespace STAJ.Entities
{
    public class LoginRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required]
        [StringLength(128, MinimumLength = 1)]
        public string Sifre { get; set; } = string.Empty;
    }
}
