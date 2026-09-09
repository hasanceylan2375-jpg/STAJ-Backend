using System.ComponentModel.DataAnnotations;

namespace STAJ.Entities
{
    public class SendMailRequest
    {
        [Required]
        [EmailAddress]
        [StringLength(320)]
        public string To { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [RegularExpression(@"^[^\r\n]*$", ErrorMessage = "Konu alanında satır sonu karakterleri kullanılamaz.")]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(10000)]
        public string Body { get; set; } = string.Empty;
    }
}
