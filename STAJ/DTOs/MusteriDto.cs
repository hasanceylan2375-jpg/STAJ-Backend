namespace STAJ.DTOs
{
    public class MusteriDto
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string Soyad { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ProfilFotoUrl { get; set; }
    }
}
