using System.ComponentModel.DataAnnotations;

namespace STAJ.Entities
{
    public class RefreshTokenRequest
    {
        [Required]
        [StringLength(256, MinimumLength = 32)]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
